using Api.Data;
using Api.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Migrator;

/// <summary>
/// The init container of the api app: it runs to completion before the API
/// starts, in every replica (13-app-templates.md section 5.3 in
/// tl-cloud-infrastructure). It takes a Postgres advisory lock, applies the
/// migrations of Api/Data and the seed data, and exits. The first replica
/// migrates; the others wait for the lock, find nothing to do and exit. A
/// failure keeps the revision from becoming ready, and the previous revision
/// keeps serving.
/// </summary>
internal static class Program
{
    // Any number that no other code of the product locks on.
    private const long MigrationLock = 7_206_532_101;

    private static async Task<int> Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Configuration.AddPlatformSettings();
        builder.Services.AddAppDbContext(builder.Configuration);

        using var host = builder.Build();
        var log = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Migrator");

        try
        {
            await using var scope = host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // The lock is held on a connection of its own for the whole run,
            // and released when the connection closes, also on a failure.
            await using var lockConnection = new NpgsqlConnection(db.Database.GetConnectionString());
            await lockConnection.OpenAsync();
            await using (var command = new NpgsqlCommand("SELECT pg_advisory_lock(@key)", lockConnection))
            {
                command.Parameters.AddWithValue("key", MigrationLock);
                log.LogInformation("Waiting for the migration lock");
                await command.ExecuteNonQueryAsync();
            }

            var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
            log.LogInformation("Applying {Count} migrations: {Migrations}", pending.Count, string.Join(", ", pending));
            await db.Database.MigrateAsync();
            await SeedData.ApplyAsync(db);
            log.LogInformation("The database is up to date");
            return 0;
        }
        catch (Exception e) when (e is NpgsqlException or InvalidOperationException or DbUpdateException)
        {
            log.LogCritical(e, "Migration failed");
            return 1;
        }
    }
}
