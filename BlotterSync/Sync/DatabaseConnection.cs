using Npgsql;

namespace BlotterSync.Sync;

public static class DatabaseConnection
{
    public static string? ResolveCloudConnectionString(IConfiguration configuration)
    {
        return Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("SupabaseConnection");
    }

    public static string ResolveLocalConnectionString(IConfiguration configuration, string contentRootPath)
    {
        var configured = configuration.GetConnectionString("LocalConnection");
        var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(
            string.IsNullOrWhiteSpace(configured) ? "Data Source=App_Data/blottersync-local.db" : configured);

        if (!Path.IsPathRooted(builder.DataSource))
        {
            builder.DataSource = Path.Combine(contentRootPath, builder.DataSource);
        }

        var directory = Path.GetDirectoryName(builder.DataSource);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return builder.ConnectionString;
    }

    public static bool IsPostgreSql(string? provider, string? conn)
    {
        if (string.Equals(provider, "PostgreSQL", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "Supabase", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(conn))
        {
            return false;
        }

        return conn.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || conn.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
            || conn.Contains("Host=", StringComparison.OrdinalIgnoreCase)
            || conn.Contains("Port=5432", StringComparison.OrdinalIgnoreCase)
            || conn.Contains("Port=6543", StringComparison.OrdinalIgnoreCase)
            || conn.Contains("supabase.co", StringComparison.OrdinalIgnoreCase)
            || conn.Contains("pooler.supabase.com", StringComparison.OrdinalIgnoreCase)
            || conn.Contains("Username=postgres", StringComparison.OrdinalIgnoreCase)
            || conn.Contains("User Id=postgres", StringComparison.OrdinalIgnoreCase);
    }

    public static string FormatNpgsqlConnectionString(string rawConnection)
    {
        if (string.IsNullOrWhiteSpace(rawConnection))
        {
            return rawConnection;
        }

        try
        {
            NpgsqlConnectionStringBuilder builder;

            if (rawConnection.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
                rawConnection.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(rawConnection);
                var userInfo = uri.UserInfo.Split(':');
                var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty;
                var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
                var database = uri.AbsolutePath.TrimStart('/');

                if (string.IsNullOrEmpty(database))
                {
                    database = "postgres";
                }

                builder = new NpgsqlConnectionStringBuilder
                {
                    Host = uri.Host,
                    Port = uri.Port > 0 ? uri.Port : 5432,
                    Database = database,
                    Username = username,
                    Password = password,
                    SslMode = SslMode.Require
                };
            }
            else
            {
                builder = new NpgsqlConnectionStringBuilder(rawConnection);

                if (!rawConnection.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase) &&
                    !rawConnection.Contains("SslMode", StringComparison.OrdinalIgnoreCase))
                {
                    builder.SslMode = SslMode.Require;
                }
            }

            if (builder.Port == 6543)
            {
                builder.Multiplexing = false;
            }

            // Fail fast when the internet is down instead of hanging the sync loop.
            if (!rawConnection.Contains("Timeout", StringComparison.OrdinalIgnoreCase))
            {
                builder.Timeout = 10;
            }

            return builder.ConnectionString;
        }
        catch
        {
            return rawConnection;
        }
    }
}
