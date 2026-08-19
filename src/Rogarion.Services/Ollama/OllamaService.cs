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

    public Task<IReadOnlyList<string>> GetInstalledModelsAsync(CancellationToken cancellationToken = default)
    {
        // Implemented in Milestone 2.
        return Task.FromResult<IReadOnlyList<string>>([]);
    }
}
