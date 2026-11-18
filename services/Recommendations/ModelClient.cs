using System.Net.Http.Headers;

namespace Recommendations;

public interface IModelClient
{
    Task<ModelReply> GenerateAsync(string prompt);
}

// Sends prompts to the AI model on port 8003.
public class ModelClient : IModelClient
{
    private readonly HttpClient _http;
    private readonly LlmSettings _settings;

    public ModelClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _settings = config.GetSection("Llm").Get<LlmSettings>() ?? new LlmSettings();

        // The model is slow sometimes, so we never time out on it.
        _http.Timeout = Timeout.InfiniteTimeSpan;
    }

    public async Task<ModelReply> GenerateAsync(string prompt)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.Url)
        {
            Content = JsonContent.Create(new { model = _settings.Model, prompt }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ModelReply>()
            ?? throw new InvalidOperationException("The AI model sent an empty reply.");
    }
}
