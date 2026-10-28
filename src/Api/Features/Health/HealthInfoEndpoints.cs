using System.Reflection;

namespace Api.Features.Health;

/// <summary>
/// GET /api/health/info, without sign-in: the API's name, version and
/// environment. Called through the front end, it shows that the /api proxy
/// reaches the API (docs/getting-started.md).
/// </summary>
public static class HealthInfoEndpoints
{
    public sealed record HealthInfo(string Name, string Version, string Environment);

    private static readonly string Version =
        typeof(HealthInfoEndpoints).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

    public static void MapHealthInfo(this IEndpointRouteBuilder api) =>
        api.MapGet("/health/info", (IConfiguration configuration) =>
            new HealthInfo("api", Version, configuration["Platform:Environment"]!))
            .WithTags("Health");
}
