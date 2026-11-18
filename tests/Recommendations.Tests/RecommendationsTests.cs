using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Recommendations;

public class RecommendationsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly ModelReply FakeModelReply = new(
        "practice-model-1",
        [
            new("P-104", 0.91, "Handy for early-morning trail runs."),
            new("P-105", 0.35, "Useful when the weather turns."),
            new("P-106", 0.77, "Takes strain off the knees on descents."),
            new("P-107", 0.12, "A warm layer for cold starts."),
            new("P-110", 0.64, "Carries water and snacks on longer runs."),
        ],
        new ModelUsage(80, 70));

    private sealed class FakeModelClient : IModelClient
    {
        public Task<ModelReply> GenerateAsync(string prompt) => Task.FromResult(FakeModelReply);
    }

    private readonly WebApplicationFactory<Program> _factory;

    public RecommendationsTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Returns_top_three_products_with_names_and_reasons()
    {
        var client = _factory
            .WithWebHostBuilder(host => host.ConfigureTestServices(services =>
                services.AddSingleton<IModelClient>(new FakeModelClient())))
            .CreateClient();

        var response = await client.PostAsJsonAsync("/recommend", new
        {
            customer = new { id = "C-001", name = "Jordan Lee", email = "jordan.lee@example.com" },
            purchased = new[] { "Trail Running Shoes", "Wool Hiking Socks" },
            limit = 3,
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var recs = body.GetProperty("recommendations").EnumerateArray().ToList();

        Assert.Equal(new[] { "P-104", "P-106", "P-110" },
            recs.Select(r => r.GetProperty("product_id").GetString()).ToArray());
        Assert.Equal(new[] { "Headlamp", "Trekking Poles", "Daypack 20L" },
            recs.Select(r => r.TryGetProperty("name", out var name) ? name.GetString() : null).ToArray());
        Assert.All(recs, r => Assert.False(string.IsNullOrEmpty(r.GetProperty("reason").GetString())));
    }
}
