using Api.Platform;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Api.Tests;

public class PlatformConfigurationTests
{
    [Fact]
    public void Maps_a_database_url_to_a_connection_string()
    {
        var connectionString = PlatformConfiguration.ToConnectionString(
            "postgresql://app%40x:p%3Ass@db.example.com:6543/my-db?sslmode=require&channel_binding=require");

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        Assert.Equal("db.example.com", builder.Host);
        Assert.Equal(6543, builder.Port);
        Assert.Equal("my-db", builder.Database);
        Assert.Equal("app@x", builder.Username);
        Assert.Equal("p:ss", builder.Password);
        Assert.Equal(SslMode.Require, builder.SslMode);
        Assert.Equal(ChannelBinding.Require, builder.ChannelBinding);
    }

    [Fact]
    public void Uses_port_5432_when_the_url_has_none()
    {
        var builder = new NpgsqlConnectionStringBuilder(PlatformConfiguration.ToConnectionString("postgres://app:app@postgres/app"));

        Assert.Equal(5432, builder.Port);
    }

    [Fact]
    public void Keeps_a_connection_string_as_it_is()
    {
        const string connectionString = "Host=localhost;Database=app;Username=app;Password=app";

        Assert.Equal(connectionString, PlatformConfiguration.ToConnectionString(connectionString));
    }

    [Fact]
    public void Maps_the_platform_settings()
    {
        var configuration = Configure(new() { ["DATABASE_URL"] = "postgres://app:app@postgres/app", ["PLATFORM_ENVIRONMENT"] = "dev" });

        Assert.Equal("dev", configuration["Platform:Environment"]);
        Assert.Equal("postgres", new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("Database")).Host);
    }

    [Fact]
    public void Names_the_environment_local_when_PLATFORM_ENVIRONMENT_is_unset()
    {
        var configuration = Configure(new() { ["DATABASE_URL"] = "postgres://app:app@postgres/app" });

        Assert.Equal(PlatformConfiguration.LocalEnvironment, configuration["Platform:Environment"]);
    }

    [Fact]
    public void Throws_when_DATABASE_URL_is_unset()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Configure([]));

        Assert.Contains("DATABASE_URL", exception.Message, StringComparison.Ordinal);
    }

    private static ConfigurationManager Configure(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(settings);
        configuration.AddPlatformSettings();
        return configuration;
    }
}
