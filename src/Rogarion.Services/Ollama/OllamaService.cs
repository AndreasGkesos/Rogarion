using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Rogarion.Core.Interfaces;

namespace Rogarion.Services.Ollama;

public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;

    public OllamaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<string>> GetInstalledModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<TagsResponse>("/api/tags", cancellationToken);
            return response?.Models?.Select(m => m.Name).ToList() ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
        catch (TaskCanceledException)
        {
            return [];
        }
    }

    private sealed class TagsResponse
    {
        [JsonPropertyName("models")]
        public List<ModelEntry>? Models { get; set; }
    }

    private sealed class ModelEntry
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
