using System.Net.Sockets;
using BlotterSync.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BlotterSync.Sync;

public class SyncStatus
{
    public bool Enabled { get; set; }
    public bool CloudOnline { get; set; }
    public bool Bootstrapped { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public string? LastError { get; set; }
}

/// <summary>
/// Copies local changes to Supabase in the background. When the internet is down
/// the changes wait in the outbox and are pushed once Supabase is reachable again.
/// </summary>
public class CloudSyncService : BackgroundService
{
    public const int MaxAttempts = 10;

    // Rows created before the first download from Supabase start at this id so they
    // cannot collide with ids that already exist in the cloud.
    private const int OfflineIdOffset = 1_000_000;
    private const int BatchSize = 200;
    private const string BootstrappedKey = "BootstrappedAt";
    private const string IdOffsetKey = "LocalIdOffsetApplied";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDbContextFactory<CloudBlotterSyncContext> _cloudFactory;
    private readonly SyncStatus _status;
    private readonly ILogger<CloudSyncService> _logger;
    private readonly TimeSpan _interval;
    private readonly bool _ensureCloudSchema;
    private readonly SemaphoreSlim _cycleLock = new(1, 1);
    private bool _cloudSchemaEnsured;

    public CloudSyncService(
        IServiceScopeFactory scopeFactory,
        IDbContextFactory<CloudBlotterSyncContext> cloudFactory,
        SyncStatus status,
        IConfiguration configuration,
        ILogger<CloudSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _cloudFactory = cloudFactory;
        _status = status;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(Math.Max(2, configuration.GetValue("Sync:IntervalSeconds", 10)));
        _ensureCloudSchema = configuration.GetValue("Database:EnsureCreated", false);
        _status.Enabled = true;
    }

    /// <summary>Creates the local database and, on first run, downloads the existing Supabase data.</summary>
    public async Task InitializeLocalStoreAsync(CancellationToken cancellationToken)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var local = scope.ServiceProvider.GetRequiredService<LocalBlotterSyncContext>();
            await local.Database.EnsureCreatedAsync(cancellationToken);
            await local.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
            _status.Bootstrapped = await local.SyncState.AnyAsync(s => s.Key == BootstrappedKey, cancellationToken);
        }

        if (_status.Bootstrapped)
        {
            return;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        try
        {
            await RunCycleAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _status.LastError = "Timed out connecting to Supabase.";
        }

        if (!_status.Bootstrapped)
        {
            _logger.LogWarning(
                "Supabase is unreachable on first start. Running from the local database; existing cloud data will be downloaded when the connection returns.");
            await ApplyOfflineIdOffsetAsync(cancellationToken);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);
                await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in the Supabase sync loop.");
            }
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        await _cycleLock.WaitAsync(cancellationToken);
        _status.LastAttemptAt = DateTime.UtcNow;

        try
        {
            if (!_status.Bootstrapped)
            {
                await BootstrapAsync(cancellationToken);
            }

            while (await PushPendingAsync(cancellationToken) == BatchSize)
            {
            }

            if (!_status.CloudOnline)
            {
                _logger.LogInformation("Supabase connection restored.");
            }

            _status.CloudOnline = true;
            _status.LastSuccessAt = DateTime.UtcNow;
            _status.LastError = null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            if (_status.CloudOnline || _status.LastSuccessAt == null && _status.LastError == null)
            {
                _logger.LogWarning("Supabase unreachable, working offline: {Message}", ex.GetBaseException().Message);
            }

            _status.CloudOnline = false;
            _status.LastError = ex.GetBaseException().Message;
        }
        finally
        {
            _cycleLock.Release();
        }
    }

    private async Task EnsureCloudSchemaAsync(CloudBlotterSyncContext cloud, CancellationToken cancellationToken)
    {
        if (_ensureCloudSchema && !_cloudSchemaEnsured)
        {
            await cloud.Database.EnsureCreatedAsync(cancellationToken);
            _cloudSchemaEnsured = true;
        }
    }

    private async Task BootstrapAsync(CancellationToken cancellationToken)
    {
        await using var cloud = await _cloudFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCloudSchemaAsync(cloud, cancellationToken);

        using var scope = _scopeFactory.CreateScope();
        var local = scope.ServiceProvider.GetRequiredService<LocalBlotterSyncContext>();
        local.SuppressOutbox = true;

        foreach (var type in SyncedEntityTypes.InDependencyOrder)
        {
            await InvokeGenericAsync(nameof(PullRowsAsync), type, cloud, local, cancellationToken);
        }

        await MergeOfflineCategoriesAsync(local, cancellationToken);

        local.SyncState.Add(new SyncStateEntry { Key = BootstrappedKey, Value = DateTime.UtcNow.ToString("O") });
        await local.SaveChangesAsync(cancellationToken);

        _status.Bootstrapped = true;
        _logger.LogInformation("Downloaded existing Supabase data into the local database.");
    }

    private async Task PullRowsAsync<T>(CloudBlotterSyncContext cloud, LocalBlotterSyncContext local, CancellationToken cancellationToken)
        where T : class
    {
        var keyName = KeyName(local, typeof(T));
        var remoteRows = await cloud.Set<T>().AsNoTracking().ToListAsync(cancellationToken);
        var localRows = (await local.Set<T>().ToListAsync(cancellationToken))
            .ToDictionary(row => KeyOf(local, row, keyName));

        foreach (var remote in remoteRows)
        {
            if (localRows.TryGetValue(KeyOf(cloud, remote, keyName), out var existing))
            {
                local.Entry(existing).CurrentValues.SetValues(remote);
            }
            else
            {
                local.Add(remote);
            }
        }
    }

    /// <summary>
    /// Categories seeded while offline on first start duplicate the cloud ones by name.
    /// Point local records at the cloud category and drop the local duplicate.
    /// </summary>
    private static async Task MergeOfflineCategoriesAsync(LocalBlotterSyncContext local, CancellationToken cancellationToken)
    {
        var categories = local.ChangeTracker.Entries<Category>().ToList();
        var cloudByName = categories
            .Where(e => e.State == EntityState.Added || e.Entity.CategoryId < OfflineIdOffset)
            .GroupBy(e => e.Entity.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Entity, StringComparer.OrdinalIgnoreCase);

        foreach (var offline in categories.Where(e => e.State != EntityState.Added && e.Entity.CategoryId >= OfflineIdOffset))
        {
            if (!cloudByName.TryGetValue(offline.Entity.Name, out var cloudCategory))
            {
                continue;
            }

            var records = await local.BlotterRecords
                .Where(r => r.CategoryId == offline.Entity.CategoryId)
                .ToListAsync(cancellationToken);

            foreach (var record in records)
            {
                record.CategoryId = cloudCategory.CategoryId;
            }

            local.Categories.Remove(offline.Entity);
        }
    }

    private async Task ApplyOfflineIdOffsetAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var local = scope.ServiceProvider.GetRequiredService<LocalBlotterSyncContext>();

        if (await local.SyncState.AnyAsync(s => s.Key == IdOffsetKey, cancellationToken))
        {
            return;
        }

        foreach (var type in SyncedEntityTypes.InDependencyOrder)
        {
            var table = local.Model.FindEntityType(type)!.GetTableName()!;
            await local.Database.ExecuteSqlRawAsync(
                "UPDATE sqlite_sequence SET seq = {1} WHERE name = {0} AND seq < {1}",
                new object[] { table, OfflineIdOffset }, cancellationToken);
            await local.Database.ExecuteSqlRawAsync(
                "INSERT INTO sqlite_sequence (name, seq) SELECT {0}, {1} WHERE NOT EXISTS (SELECT 1 FROM sqlite_sequence WHERE name = {0})",
                new object[] { table, OfflineIdOffset }, cancellationToken);
        }

        local.SyncState.Add(new SyncStateEntry { Key = IdOffsetKey, Value = DateTime.UtcNow.ToString("O") });
        await local.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Pushes one batch of the outbox. Returns how many entries were handled.</summary>
    private async Task<int> PushPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var local = scope.ServiceProvider.GetRequiredService<LocalBlotterSyncContext>();

        var batch = await local.SyncOutbox
            .Where(e => e.Attempts < MaxAttempts)
            .OrderBy(e => e.Id)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (batch.Count == 0)
        {
            // Still prove Supabase is reachable so the status shown to users is accurate.
            await using var probe = await _cloudFactory.CreateDbContextAsync(cancellationToken);
            await EnsureCloudSchemaAsync(probe, cancellationToken);
            await probe.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            return 0;
        }

        try
        {
            await PushEntriesAsync(local, batch, cancellationToken);
            local.SyncOutbox.RemoveRange(batch);
        }
        catch (Exception ex) when (IsRejectedByServer(ex))
        {
            // Supabase is reachable but refused part of the batch. Push rows one by one
            // so a single bad row does not hold back everything after it.
            foreach (var group in batch.GroupBy(e => (e.EntityType, e.EntityKey)))
            {
                try
                {
                    await PushEntriesAsync(local, group.ToList(), cancellationToken);
                    local.SyncOutbox.RemoveRange(group);
                }
                catch (Exception rowEx) when (IsRejectedByServer(rowEx))
                {
                    var message = rowEx.GetBaseException().Message;
                    _logger.LogWarning("Supabase rejected {Type} #{Key}: {Message}", group.Key.EntityType, group.Key.EntityKey, message);

                    foreach (var entry in group)
                    {
                        entry.Attempts++;
                        entry.LastError = message.Length > 1000 ? message[..1000] : message;
                    }
                }
            }
        }

        await local.SaveChangesAsync(cancellationToken);
        return batch.Count;
    }

    private async Task PushEntriesAsync(LocalBlotterSyncContext local, List<SyncOutboxEntry> entries, CancellationToken cancellationToken)
    {
        await using var cloud = await _cloudFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCloudSchemaAsync(cloud, cancellationToken);

        var keysByType = entries
            .GroupBy(e => e.EntityType)
            .ToDictionary(g => g.Key, g => g.Select(e => e.EntityKey).Distinct().ToList());

        var insertedTypes = new List<Type>();
        foreach (var type in SyncedEntityTypes.InDependencyOrder)
        {
            if (keysByType.TryGetValue(type.Name, out var keys) &&
                await InvokeGenericAsync<bool>(nameof(CopyRowsAsync), type, cloud, local, keys, cancellationToken))
            {
                insertedTypes.Add(type);
            }
        }

        await cloud.SaveChangesAsync(cancellationToken);

        foreach (var type in insertedTypes)
        {
            await AdvanceCloudIdentityAsync(cloud, type, cancellationToken);
        }
    }

    /// <summary>
    /// Makes the Supabase rows for <paramref name="keys"/> match the local rows:
    /// updates or inserts rows that exist locally and deletes the ones removed locally.
    /// Returns true when any row was inserted.
    /// </summary>
    private async Task<bool> CopyRowsAsync<T>(CloudBlotterSyncContext cloud, LocalBlotterSyncContext local, List<int> keys, CancellationToken cancellationToken)
        where T : class
    {
        var keyName = KeyName(local, typeof(T));

        var localRows = await local.Set<T>().AsNoTracking()
            .Where(e => keys.Contains(EF.Property<int>(e, keyName)))
            .ToListAsync(cancellationToken);
        var remoteRows = (await cloud.Set<T>()
                .Where(e => keys.Contains(EF.Property<int>(e, keyName)))
                .ToListAsync(cancellationToken))
            .ToDictionary(row => KeyOf(cloud, row, keyName));

        var inserted = false;
        foreach (var row in localRows)
        {
            if (remoteRows.Remove(KeyOf(local, row, keyName), out var remote))
            {
                cloud.Entry(remote).CurrentValues.SetValues(row);
            }
            else
            {
                cloud.Add(row);
                inserted = true;
            }
        }

        cloud.RemoveRange(remoteRows.Values);
        return inserted;
    }

    /// <summary>Rows are inserted with explicit ids, so move the Supabase identity past them.</summary>
    private async Task AdvanceCloudIdentityAsync(CloudBlotterSyncContext cloud, Type type, CancellationToken cancellationToken)
    {
        try
        {
            var entityType = cloud.Model.FindEntityType(type)!;
            var schema = entityType.GetSchema() ?? "public";
            var table = entityType.GetTableName()!;
            var column = entityType.FindPrimaryKey()!.Properties[0].GetColumnName();
            var qualified = $"\"{schema}\".\"{table}\"";

            // Identifiers come from the EF model, not from user input.
#pragma warning disable EF1002
            await cloud.Database.ExecuteSqlRawAsync(
                $"SELECT setval(pg_get_serial_sequence('{qualified}', '{column}'), " +
                $"GREATEST((SELECT COALESCE(MAX(\"{column}\"), 0) FROM {qualified}), 1))",
                cancellationToken);
#pragma warning restore EF1002
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not advance the Supabase identity for {Type}.", type.Name);
        }
    }

    private Task InvokeGenericAsync(string method, Type type, params object[] args)
    {
        return (Task)GetType()
            .GetMethod(method, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(type)
            .Invoke(this, args)!;
    }

    private Task<TResult> InvokeGenericAsync<TResult>(string method, Type type, params object[] args)
    {
        return (Task<TResult>)InvokeGenericAsync(method, type, args);
    }

    private static string KeyName(DbContext context, Type type)
    {
        return context.Model.FindEntityType(type)!.FindPrimaryKey()!.Properties[0].Name;
    }

    private static int KeyOf(DbContext context, object entity, string keyName)
    {
        return (int)context.Entry(entity).Property(keyName).CurrentValue!;
    }

    /// <summary>
    /// True when Supabase answered but refused the data (constraint, type or schema error).
    /// Network failures and server-availability errors are false, so the batch is retried later.
    /// </summary>
    private static bool IsRejectedByServer(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is PostgresException pg)
            {
                return !(pg.SqlState.StartsWith("08") || pg.SqlState.StartsWith("53") || pg.SqlState.StartsWith("57"));
            }

            if (current is SocketException or TimeoutException or IOException or NpgsqlException)
            {
                return false;
            }
        }

        return false;
    }
}
