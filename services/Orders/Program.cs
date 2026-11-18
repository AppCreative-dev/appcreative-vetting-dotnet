// Orders service.
//
// Returns an order together with product recommendations.
// Runs on port 8001 and calls the recommendations service on port 8002.

using System.Text.Json;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// --- Telemetry ---------------------------------------------------------------
// Traces from this service are sent to the OpenTelemetry Collector on port 4318,
// which forwards them to Grafana.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("orders"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4318/v1/traces");
            options.Protocol = OtlpExportProtocol.HttpProtobuf;
        }));

builder.Services.AddHttpClient("recommendations", client =>
{
    client.BaseAddress = new Uri("http://localhost:8002");
    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

// --- Practice data -----------------------------------------------------------
var customers = new Dictionary<string, Customer>
{
    ["C-001"] = new("C-001", "Jordan Lee", "jordan.lee@example.com"),
    ["C-002"] = new("C-002", "Sam Rivera", "sam.rivera@example.com"),
    ["C-003"] = new("C-003", "Alex Chen", "alex.chen@example.com"),
};

var orders = new Dictionary<string, Order>
{
    ["A1001"] = new("C-001", ["Trail Running Shoes", "Wool Hiking Socks"]),
    ["A1002"] = new("C-002", ["Insulated Water Bottle"]),
    ["A1003"] = new("C-003", ["Snowboard", "Snowboard Bindings"]),
};

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/orders/{orderId}", async (string orderId, IHttpClientFactory httpClients, ILogger<Program> log) =>
{
    if (!orders.TryGetValue(orderId, out var order))
    {
        return Results.NotFound(new { detail = "order not found" });
    }

    var customer = customers[order.CustomerId];
    log.LogInformation("Order {OrderId} requested", orderId);

    var client = httpClients.CreateClient("recommendations");
    var response = await client.PostAsJsonAsync("/recommend", new
    {
        customer,
        purchased = order.Items,
        limit = 3,
    });
    response.EnsureSuccessStatusCode();
    var body = await response.Content.ReadFromJsonAsync<JsonElement>();

    return Results.Ok(new
    {
        order_id = orderId,
        items = order.Items,
        recommendations = body.GetProperty("recommendations"),
    });
});

app.Run();

record Customer(string Id, string Name, string Email);

record Order(string CustomerId, List<string> Items);
