using Microsoft.AspNetCore.WebUtilities;
using Npgsql;

namespace Api.Platform;

/// <summary>
/// The one place that knows the platform's setting names
/// (13-app-templates.md section 6.1 in tl-cloud-infrastructure). It turns them
/// into the configuration keys the rest of the app reads, so nothing else reads
/// an environment variable. Add a mapping here for each setting or secret the
/// product adds, such as REDIS_URL when its stamp adds a cache.
/// </summary>
public static class PlatformConfiguration
{
    /// <summary>The environment name when PLATFORM_ENVIRONMENT is unset.</summary>
    public const string LocalEnvironment = "local";

    /// <summary>
    /// Maps DATABASE_URL to ConnectionStrings:Database, and PLATFORM_ENVIRONMENT
    /// to Platform:Environment. Throws when DATABASE_URL is unset: neither the
    /// API nor the migrator runs without Postgres.
    /// </summary>
    public static void AddPlatformSettings(this IConfigurationManager configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var databaseUrl = configuration["DATABASE_URL"];
        if (string.IsNullOrWhiteSpace(databaseUrl))
        {
            throw new InvalidOperationException(
                "DATABASE_URL is not set. The stamp sets it; locally, .env sets it for docker compose (docs/configuration.md).");
        }

        var environment = configuration["PLATFORM_ENVIRONMENT"];
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = ToConnectionString(databaseUrl),
            ["Platform:Environment"] = string.IsNullOrWhiteSpace(environment) ? LocalEnvironment : environment,
        });
    }

    /// <summary>
    /// Turns a URL such as postgresql://user:password@host:5432/db?sslmode=require,
    /// the form the stamp's DATABASE_URL has, into an Npgsql connection string.
    /// A value that is already a connection string is returned as it is.
    /// </summary>
    internal static string ToConnectionString(string databaseUrl)
    {
        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("postgres" or "postgresql"))
        {
            return databaseUrl;
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
        };

        var user = uri.UserInfo.Split(':', 2);
        if (user[0].Length > 0)
        {
            builder.Username = Uri.UnescapeDataString(user[0]);
        }
        if (user.Length == 2)
        {
            builder.Password = Uri.UnescapeDataString(user[1]);
        }

        foreach (var (key, value) in QueryHelpers.ParseQuery(uri.Query))
        {
            builder[key switch
            {
                "sslmode" => "SSL Mode",
                "channel_binding" => "Channel Binding",
                _ => key,
            }] = value.ToString();
        }

        return builder.ConnectionString;
    }
}
