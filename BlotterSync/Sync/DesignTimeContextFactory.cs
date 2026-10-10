using BlotterSync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BlotterSync.Sync;

/// <summary>
/// Keeps `dotnet ef migrations` targeting Supabase (PostgreSQL). At runtime the app
/// resolves BlotterSyncContext to the local SQLite store instead.
/// </summary>
public class DesignTimeContextFactory : IDesignTimeDbContextFactory<BlotterSyncContext>
{
    public BlotterSyncContext CreateDbContext(string[] args)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connection = DatabaseConnection.ResolveCloudConnectionString(configuration)
            ?? throw new InvalidOperationException("No Supabase connection string configured.");

        var options = new DbContextOptionsBuilder<BlotterSyncContext>()
            .UseNpgsql(DatabaseConnection.FormatNpgsqlConnectionString(connection))
            .Options;

        return new BlotterSyncContext(options);
    }
}
