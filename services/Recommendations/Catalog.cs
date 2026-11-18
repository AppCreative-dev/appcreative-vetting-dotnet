namespace Recommendations;

public static class Catalog
{
    public static readonly List<Product> Products =
    [
        new("P-101", "Wool Hiking Socks", 1),
        new("P-102", "Trail Running Shoes", null),
        new("P-103", "Insulated Water Bottle", 2),
        new("P-104", "Headlamp", 3),
        new("P-105", "Rain Shell Jacket", 4),
        new("P-106", "Trekking Poles", null),
        new("P-107", "Merino Base Layer", null),
        new("P-108", "Snowboard Wax Kit", null),
        new("P-109", "Ski Goggles", null),
        new("P-110", "Daypack 20L", null),
    ];
}

public static class Prompt
{
    public static string Build(Customer customer, List<string> purchased, List<Product> candidates)
    {
        var lines = new List<string>
        {
            "You are a helpful shopping assistant.",
            $"Customer: {customer.Name} <{customer.Email}> (id {customer.Id})",
            $"They just bought: {string.Join(", ", purchased)}",
            "Score each product below from 0 to 1 for how likely they are to want it next,",
            "and give a one-sentence reason.",
            "Products:",
        };
        lines.AddRange(candidates.Select(p => $"- {p.ProductId} | {p.Name}"));
        return string.Join("\n", lines);
    }
}

public static class Ranking
{
    // Order the model's results from best to worst.
    public static List<ModelResult> Rank(List<ModelResult> results) =>
        results.OrderBy(r => r.Score).ToList();
}
