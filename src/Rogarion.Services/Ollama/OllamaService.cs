using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rogarion.Core.Interfaces;
using Rogarion.Core.Models;

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
            return response?.Models?
                .Where(m => m.Capabilities is null || m.Capabilities.Contains("completion"))
                .Select(m => m.Name)
                .ToList() ?? [];
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

    public async Task<int?> GetContextWindowAsync(string model, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/show", new { model }, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);

            if (!doc.RootElement.TryGetProperty("model_info", out var modelInfo))
            {
                return null;
            }

            foreach (var property in modelInfo.EnumerateObject())
            {
                if (property.Name.EndsWith(".context_length", StringComparison.Ordinal)
                    && property.Value.TryGetInt32(out var contextLength))
                {
                    return contextLength;
                }
            }

            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        string model,
        IReadOnlyList<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new ChatRequest
        {
            Model = model,
            Stream = true,
            Messages = messages.Select(m => new ChatRequestMessage
            {
                Role = m.Role switch
                {
                    ChatRole.User => "user",
                    ChatRole.Assistant => "assistant",
                    ChatRole.System => "system",
                    _ => "user"
                },
                Content = m.Content
            }).ToList()
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = JsonContent.Create(request)
        };

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                yield break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var chunk = JsonSerializer.Deserialize<ChatResponse>(line);
            if (chunk?.Message?.Content is { Length: > 0 } content)
            {
                yield return content;
            }

            if (chunk?.Done == true)
            {
                yield break;
            }
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

        [JsonPropertyName("capabilities")]
        public List<string>? Capabilities { get; set; }
    }

    private sealed class ChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<ChatRequestMessage> Messages { get; set; } = [];

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private sealed class ChatRequestMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class ChatResponse
    {
        [JsonPropertyName("message")]
        public ChatRequestMessage? Message { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }
}
