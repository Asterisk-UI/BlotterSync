using BlotterSync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BlotterSync.Sync;

/// <summary>
/// A row changed locally that still has to be copied to Supabase.
/// The sync worker re-reads the current local row, so only the key is stored.
/// </summary>
public class SyncOutboxEntry
{
    public long Id { get; set; }
    public string EntityType { get; set; } = null!;
    public int EntityKey { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}

public class SyncStateEntry
{
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
}

/// <summary>
/// The SQLite store on this server. The API always reads and writes here, so it
/// keeps working when the internet is down. Every change is recorded in the outbox.
/// </summary>
public class LocalBlotterSyncContext : BlotterSyncContext
{
    public LocalBlotterSyncContext(DbContextOptions<LocalBlotterSyncContext> options)
        : base(options)
    {
    }

    public DbSet<SyncOutboxEntry> SyncOutbox { get; set; }

    public DbSet<SyncStateEntry> SyncState { get; set; }

    /// <summary>Set while copying rows down from Supabase so they are not echoed back.</summary>
    public bool SuppressOutbox { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SyncOutboxEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.HasIndex(e => new { e.EntityType, e.EntityKey });
        });

        modelBuilder.Entity<SyncStateEntry>(entity => entity.HasKey(e => e.Key));
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        return SaveChangesAsync(acceptAllChangesOnSuccess).GetAwaiter().GetResult();
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var changed = SuppressOutbox ? new List<EntityEntry>() : CaptureSyncedChanges();
        if (changed.Count == 0)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        // Record keys of deleted rows now; added rows only get their key after saving.
        var outbox = changed
            .Where(e => e.State == EntityState.Deleted)
            .Select(ToOutboxEntry)
            .ToList();
        var upserted = changed.Where(e => e.State != EntityState.Deleted).ToList();

        await using var transaction = Database.CurrentTransaction == null
            ? await Database.BeginTransactionAsync(cancellationToken)
            : null;

        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        outbox.AddRange(upserted.Select(ToOutboxEntry));
        SyncOutbox.AddRange(outbox);
        await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return result;
    }

    private List<EntityEntry> CaptureSyncedChanges()
    {
        return ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => SyncedEntityTypes.IsSynced(e.Metadata.ClrType))
            .ToList();
    }

    private static SyncOutboxEntry ToOutboxEntry(EntityEntry entry)
    {
        var keyProperty = entry.Metadata.FindPrimaryKey()!.Properties[0];

        return new SyncOutboxEntry
        {
            EntityType = entry.Metadata.ClrType.Name,
            EntityKey = (int)entry.Property(keyProperty.Name).CurrentValue!,
            CreatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>The Supabase copy. Only the sync worker talks to it.</summary>
public class CloudBlotterSyncContext : BlotterSyncContext
{
    public CloudBlotterSyncContext(DbContextOptions<CloudBlotterSyncContext> options)
        : base(options)
    {
    }
}

public static class SyncedEntityTypes
{
    // Parents first so foreign keys exist in Supabase before the rows that use them.
    public static readonly Type[] InDependencyOrder =
    {
        typeof(Category),
        typeof(Officer),
        typeof(Resident),
        typeof(Announcement),
        typeof(BlotterRecord)
    };

    public static bool IsSynced(Type type) => InDependencyOrder.Contains(type);
}
