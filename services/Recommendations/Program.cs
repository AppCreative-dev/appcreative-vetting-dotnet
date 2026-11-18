// Recommendations service.
//
// Asks an AI model which products a customer is likely to want next.
// Runs on port 8002 and calls the AI model on port 8003.

using Recommendations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<IModelClient, ModelClient>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/recommend", async (RecommendRequest req, IModelClient model, ILogger<Program> log) =>
{
    var candidates = Catalog.Products.Where(p => !req.Purchased.Contains(p.Name)).ToList();
    log.LogInformation("Recommending for customer {CustomerId} from {Count} products", req.Customer.Id, candidates.Count);

    var prompt = Prompt.Build(req.Customer, req.Purchased, candidates);
    var reply = await model.GenerateAsync(prompt);
    var ranked = Ranking.Rank(reply.Results);

    // TODO: finish this feature.
    // Return the top `req.Limit` products. Each one needs its product_id,
    // its name (from Catalog) and the model's reason.
    return Results.Ok(new { recommendations = ranked });
});

app.Run();

// Lets the tests start this app.
public partial class Program { }
