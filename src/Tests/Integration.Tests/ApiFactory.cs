using Api.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(Integration.Tests.ApiFactory))]

namespace Integration.Tests;

/// <summary>
/// The API against a throw-away Postgres 17 container that the tests start
/// themselves, migrated as the migrator does it. One for all tests.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // The Postgres of docker-compose.yml and of a dev stamp.
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder(
        "postgres:17.11-alpine@sha256:266d3c257594e195b309ae9d1805e54c788f1d7b375f56bf6c367aac6c917e7b").Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        // The API reads DATABASE_URL at start, in the URL form the stamp sets.
        var b = new Npgsql.NpgsqlConnectionStringBuilder(_postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("DATABASE_URL", $"postgresql://{b.Username}:{b.Password}@{b.Host}:{b.Port}/{b.Database}");
        Environment.SetEnvironmentVariable("PLATFORM_ENVIRONMENT", "test");

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await SeedData.ApplyAsync(db);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
