using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace BookNest.Services;

/// <summary>
/// Translates a single piece of user-generated text (a review, etc.) on demand, via the
/// "Translate" button, using the same free Groq API key as Lyra. Only called when the user
/// explicitly asks for a translation — never automatically on page load.
/// </summary>
public class GroqTranslationService : ITranslationService
{
    private readonly HttpClient _httpClient;
    private readonly LyraOptions _options;
    private readonly ILogger<GroqTranslationService> _logger;

    public GroqTranslationService(HttpClient httpClient, IOptions<LyraOptions> options, ILogger<GroqTranslationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string?> TranslateAsync(string text, string targetLanguage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Groq.ApiKey) || string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        try
        {
            var request = new GroqChatRequest
            {
                Model = _options.Groq.Model,
                Temperature = 0.2,
                Messages = new List<GroqChatMessage>
                {
                    new()
                    {
                        Role = "system",
                        Content = $"You translate text to {targetLanguage}. Output ONLY the translation, with no preamble, quotes, or explanation. Preserve the original tone and meaning as closely as possible."
                    },
                    new() { Role = "user", Content = text }
                }
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Groq.ApiKey);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Groq translation request returned HTTP status {StatusCode}.", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<GroqChatResponse>(cancellationToken: cancellationToken);
            return result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            _logger.LogWarning(exception, "Translation request failed.");
            return null;
        }
    }

    private sealed class GroqChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("temperature")]
        public double Temperature { get; init; }

        [JsonPropertyName("messages")]
        public List<GroqChatMessage> Messages { get; init; } = new();
    }

    private sealed class GroqChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; init; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; init; } = string.Empty;
    }

    private sealed class GroqChatResponse
    {
        [JsonPropertyName("choices")]
        public List<GroqChoice>? Choices { get; init; }
    }

    private sealed class GroqChoice
    {
        [JsonPropertyName("message")]
        public GroqChatMessage? Message { get; init; }
    }
}
