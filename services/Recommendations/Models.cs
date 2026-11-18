using System.Text.Json.Serialization;

namespace Recommendations;

public record Customer(string Id, string Name, string Email);

public record RecommendRequest(Customer Customer, List<string> Purchased, int Limit = 3);

public record Product(string ProductId, string Name, int? BestSellerRank);

// What the AI model sends back.
public record ModelResult(
    [property: JsonPropertyName("product_id")] string ProductId,
    double Score,
    string Reason);

public record ModelUsage(
    [property: JsonPropertyName("input_tokens")] int InputTokens,
    [property: JsonPropertyName("output_tokens")] int OutputTokens);

public record ModelReply(string Model, List<ModelResult> Results, ModelUsage? Usage);

// What this service returns for each recommended product.
public record Recommendation(
    [property: JsonPropertyName("product_id")] string ProductId,
    string Name,
    string Reason);

public class LlmSettings
{
    public string Url { get; set; } = "";
    public string Model { get; set; } = "";
    public string ApiKey { get; set; } = "";
}
