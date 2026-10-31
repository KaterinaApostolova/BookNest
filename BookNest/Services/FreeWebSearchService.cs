using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace BookNest.Services;

/// <summary>
/// Searches the real, open web via Tavily's free API tier (1,000 searches/month, no credit
/// card). Authenticates with an Authorization: Bearer header, per Tavily's current API.
/// </summary>
public class FreeWebSearchService : IWebSearchService
{
    private readonly HttpClient _httpClient;
    private readonly LyraOptions _options;
    private readonly ILogger<FreeWebSearchService> _logger;

    public FreeWebSearchService(HttpClient httpClient, IOptions<LyraOptions> options, ILogger<FreeWebSearchService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string?> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Tavily.ApiKey))
        {
            _logger.LogWarning("Lyra:Tavily:ApiKey is not configured.");
            return null;
        }

        try
        {
            var request = new TavilyRequest
            {
                Query = query,
                MaxResults = 4,
                SearchDepth = "basic"
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "search")
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Tavily.ApiKey);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Tavily returned HTTP status {StatusCode}: {Body}", response.StatusCode, errorBody);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<TavilyResponse>(cancellationToken: cancellationToken);
            if (result?.Results is null || result.Results.Count == 0)
            {
                return null;
            }

            var formatted = result.Results
                .Take(4)
                .Select(item => $"Title: {item.Title}\nSnippet: {item.Content}\nURL: {item.Url}");

            return string.Join("\n\n", formatted);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Tavily search failed.");
            return null;
        }
        catch (TaskCanceledException exception)
        {
            _logger.LogWarning(exception, "Tavily search timed out.");
            return null;
        }
        catch (System.Text.Json.JsonException exception)
        {
            _logger.LogWarning(exception, "Tavily returned an invalid response.");
            return null;
        }
    }

    private sealed class TavilyRequest
    {
        [JsonPropertyName("query")]
        public string Query { get; init; } = string.Empty;

        [JsonPropertyName("max_results")]
        public int MaxResults { get; init; }

        [JsonPropertyName("search_depth")]
        public string SearchDepth { get; init; } = "basic";
    }

    private sealed class TavilyResponse
    {
        [JsonPropertyName("results")]
        public List<TavilyResult>? Results { get; init; }
    }

    private sealed class TavilyResult
    {
        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; init; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; init; } = string.Empty;
    }
}
