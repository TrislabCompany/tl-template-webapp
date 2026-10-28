using System.Net;
using System.Net.Http.Json;
using Api.Features.Health;
using Api.Features.Items;

namespace Integration.Tests;

public class ApiTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("/healthz")]
    [InlineData("/readyz")]
    public async Task Is_alive_and_ready(string path)
    {
        var response = await _client.GetAsync(new Uri(path, UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Answers_health_info_with_the_environment()
    {
        var info = await _client.GetFromJsonAsync<HealthInfoEndpoints.HealthInfo>("/api/health/info", Ct);

        Assert.Equal("api", info!.Name);
        Assert.Equal("test", info.Environment);
    }

    [Fact]
    public async Task Lists_the_seed_data()
    {
        var items = await _client.GetFromJsonAsync<List<Item>>("/api/items", Ct);

        Assert.Contains(items!, i => i.Name == "Read the README");
    }

    [Fact]
    public async Task Adds_changes_and_deletes_an_item()
    {
        var created = await _client.PostAsJsonAsync("/api/items", new { name = "  Write a test  " }, Ct);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var item = (await created.Content.ReadFromJsonAsync<Item>(Ct))!;
        Assert.Equal("Write a test", item.Name);

        var changed = await _client.PutAsJsonAsync($"/api/items/{item.Id}", new { name = "Write two tests", done = true }, Ct);
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.True((await changed.Content.ReadFromJsonAsync<Item>(Ct))!.Done);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync(new Uri($"/api/items/{item.Id}", UriKind.Relative), Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync(new Uri($"/api/items/{item.Id}", UriKind.Relative), Ct)).StatusCode);
    }

    [Fact]
    public async Task Rejects_an_item_without_a_name()
    {
        var response = await _client.PostAsJsonAsync("/api/items", new { name = " " }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
