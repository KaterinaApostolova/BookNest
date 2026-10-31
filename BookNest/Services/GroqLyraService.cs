using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace BookNest.Services;

/// <summary>
/// Talks to Groq's free-tier chat completions API (OpenAI-compatible). This is the default
/// Lyra backend: it is a plain HTTPS call, so it works identically on localhost and on a
/// deployed/hosted site — unlike a local Ollama process, it needs nothing running alongside
/// the web app itself. Get a free key (no credit card) at https://console.groq.com/keys.
/// </summary>
public class GroqLyraService : ILyraService
{
    private const string UnavailableMessage = "Lyra is currently unavailable. Please check that a Groq API key is configured and try again.";

    private readonly HttpClient _httpClient;
    private readonly IWebSearchService _webSearchService;
    private readonly LyraOptions _options;
    private readonly LyraContextBuilder _contextBuilder;
    private readonly ILogger<GroqLyraService> _logger;

    public GroqLyraService(
        HttpClient httpClient,
        IWebSearchService webSearchService,
        IOptions<LyraOptions> options,
        LyraContextBuilder contextBuilder,
        ILogger<GroqLyraService> logger)
    {
        _httpClient = httpClient;
        _webSearchService = webSearchService;
        _options = options.Value;
        _contextBuilder = contextBuilder;
        _logger = logger;
    }

    public async Task<string> AskAsync(string message, int? bookId = null, IReadOnlyList<LyraTurn>? history = null, string? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.Groq.ApiKey))
            {
                _logger.LogWarning("Lyra:Groq:ApiKey is not configured.");
                return UnavailableMessage;
            }

            var bookContext = await _contextBuilder.BuildBookContextAsync(message, bookId, userId, cancellationToken);
            var needsWebSearch = LyraContextBuilder.ShouldSearchWeb(message, bookContext);
            var webContext = needsWebSearch
                ? await _webSearchService.SearchAsync(message, cancellationToken)
                : null;
            var prompt = LyraContextBuilder.BuildPrompt(message, bookContext, webContext, history);

            var request = new GroqChatRequest
            {
                Model = _options.Groq.Model,
                Messages = new List<GroqChatMessage>
                {
                    new() { Role = "system", Content = LyraContextBuilder.SystemPrompt },
                    new() { Role = "user", Content = prompt }
                }
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Groq.ApiKey);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Groq rate limit reached.");
                return "Lyra is getting a lot of requests right now (free plan limit). Please try again in a moment.";
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Groq returned HTTP status {StatusCode}: {Body}", response.StatusCode, errorBody);
                return UnavailableMessage;
            }

            var result = await response.Content.ReadFromJsonAsync<GroqChatResponse>(cancellationToken: cancellationToken);
            var answer = result?.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrWhiteSpace(answer))
            {
                return "I could not form an answer this time. Please try asking in a different way.";
            }

            var sourceNotice = bookContext is not null && webContext is not null
                ? "Using information from BookNest and web search. "
                : bookContext is not null
                ? "Using information from BookNest. "
                : webContext is not null
                ? "Using information found through web search. "
                : string.Empty;

            return sourceNotice + answer.Trim();
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Groq is unavailable.");
            return UnavailableMessage;
        }
        catch (TaskCanceledException exception)
        {
            _logger.LogWarning(exception, "The Groq request timed out or was cancelled.");
            return UnavailableMessage;
        }
        catch (System.Text.Json.JsonException exception)
        {
            _logger.LogWarning(exception, "Groq returned an invalid response.");
            return UnavailableMessage;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Lyra failed while preparing or sending a request.");
            return UnavailableMessage;
        }
    }

    private sealed class GroqChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

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
