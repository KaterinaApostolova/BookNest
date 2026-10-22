using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BookNest.Data;
using BookNest.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookNest.Services;

public sealed class LyraOptions
{
    public OllamaOptions Ollama { get; set; } = new();
}

public sealed class OllamaOptions
{
    public string Url { get; set; } = "http://localhost:11434/";
    public string Model { get; set; } = string.Empty;
}

public class OllamaLyraService : ILyraService
{
    private const string UnavailableMessage = "Lyra is currently unavailable. Please make sure the local AI service is running and try again.";
    private const string SystemPrompt = "You are Lyra, a friendly reading assistant for BookNest. Recommend books, discuss genres, explain books, help users choose what to read, discuss authors, and help users understand BookNest features. Use BookNest information first when it is provided, then web search information. Never guess or invent a title, author, rating, review, or Reading Journey detail; if the provided sources do not contain an answer, say that it is unavailable. Clearly distinguish BookNest information, web search information, and local model knowledge. Keep answers helpful and concise.";

    private readonly BookNestDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly IWebSearchService _webSearchService;
    private readonly LyraOptions _options;
    private readonly ILogger<OllamaLyraService> _logger;

    public OllamaLyraService(
        BookNestDbContext context,
        HttpClient httpClient,
        IWebSearchService webSearchService,
        IOptions<LyraOptions> options,
        ILogger<OllamaLyraService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _webSearchService = webSearchService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> AskAsync(string message, int? bookId = null, IReadOnlyList<LyraTurn>? history = null, string? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.Ollama.Model))
            {
                _logger.LogWarning("Lyra:Ollama:Model is not configured.");
                return UnavailableMessage;
            }

            var bookContext = await BuildBookContextAsync(message, bookId, userId, cancellationToken);
            var needsWebSearch = ShouldSearchWeb(message, bookContext);
            var webContext = needsWebSearch
                ? await _webSearchService.SearchAsync(message, cancellationToken)
                : null;
            var prompt = BuildPrompt(message, bookContext, webContext, history);
            var request = new OllamaRequest
            {
                Model = _options.Ollama.Model,
                System = SystemPrompt,
                Prompt = prompt,
                Stream = false
            };

            using var response = await _httpClient.PostAsJsonAsync("api/generate", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama returned HTTP status {StatusCode}.", response.StatusCode);
                return UnavailableMessage;
            }

            var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(result?.Response))
            {
                return "I could not form an answer this time. Please try asking in a different way.";
            }

            var sourceNotice = needsWebSearch && webContext is null
                ? "I could not perform an external web search, so this answer is based on Lyra's local model knowledge. "
                : bookContext is not null
                    ? "Using information from BookNest. "
                    : webContext is not null
                        ? "Using information found through web search. "
                        : "Based on Lyra's local model knowledge. ";

            return sourceNotice + result.Response.Trim();
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Ollama is unavailable.");
            return UnavailableMessage;
        }
        catch (TaskCanceledException exception)
        {
            _logger.LogWarning(exception, "The Ollama request timed out or was cancelled.");
            return UnavailableMessage;
        }
        catch (System.Text.Json.JsonException exception)
        {
            _logger.LogWarning(exception, "Ollama returned an invalid response.");
            return UnavailableMessage;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Lyra failed while preparing or sending a request.");
            return UnavailableMessage;
        }
    }

    private async Task<string?> BuildBookContextAsync(string message, int? bookId, string? userId, CancellationToken cancellationToken)
    {
        var book = bookId.HasValue
            ? await _context.Books
                .AsNoTracking()
                .Include(book => book.Authors)
                .Include(book => book.Genres)
                .Include(book => book.Reviews)
                .Include(book => book.ReadingLists)
                    .ThenInclude(readingList => readingList.ReadingStatus)
                .FirstOrDefaultAsync(book => book.Id == bookId.Value, cancellationToken)
            : await FindBookAsync(message, cancellationToken);

        if (book is null)
        {
            return null;
        }

        var authors = book.Authors.Any()
            ? string.Join(", ", book.Authors.Select(author => author.Name))
            : "Unknown";
        var genres = book.Genres.Any()
            ? string.Join(", ", book.Genres.Select(genre => genre.Name))
            : "Unknown";
        var description = string.IsNullOrWhiteSpace(book.Description) ? "Not available" : book.Description;
        var rating = book.AverageRating.HasValue ? book.AverageRating.Value.ToString("0.0") : "Not rated";
        var reviews = book.Reviews.Count == 0
            ? "No reviews yet"
            : string.Join(" | ", book.Reviews.OrderByDescending(review => review.CreatedAt).Take(3)
                .Select(review => $"{review.Rating}/5: {review.Content}"));
        var journeyStatus = userId is null
            ? "Unknown"
            : book.ReadingLists.FirstOrDefault(item => item.UserId == userId)?.ReadingStatus?.Name ?? "Not in this user's Reading Journey";

        return $"Source: BookNest\nTitle: {book.Title}\nAuthors: {authors}\nGenres: {genres}\nDescription: {description}\nAverage rating: {rating}\nReviews: {reviews}\nUser's Reading Journey status: {journeyStatus}";
    }

    private async Task<BookNest.Models.Book?> FindBookAsync(string message, CancellationToken cancellationToken)
    {
        var words = message
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(word => word.Length >= 4)
            .Select(word => word.Trim('.', ',', '?', '!', ':', ';', '"', '\''))
            .Where(word => word.Length >= 4)
            .ToList();

        if (words.Count == 0)
        {
            return null;
        }

        var books = await _context.Books
            .AsNoTracking()
            .Include(book => book.Authors)
            .Include(book => book.Genres)
            .Include(book => book.Reviews)
            .Include(book => book.ReadingLists)
                .ThenInclude(readingList => readingList.ReadingStatus)
            .ToListAsync(cancellationToken);

        return books.FirstOrDefault(book => words.Any(word =>
            book.Title.Contains(word, StringComparison.OrdinalIgnoreCase) ||
            book.Authors.Any(author => author.Name.Contains(word, StringComparison.OrdinalIgnoreCase))));
    }

    private static bool ShouldSearchWeb(string message, string? bookContext)
    {
        if (bookContext is not null && !ContainsAny(message, "current", "latest", "recent", "today", "news", "now", "upcoming"))
        {
            return false;
        }

        return ContainsAny(message, "current", "latest", "recent", "today", "news", "now", "upcoming", "published", "release", "biography", "isbn", "book", "novel", "author", "writer") || bookContext is null;
    }

    private static string BuildPrompt(string message, string? bookContext, string? webContext, IReadOnlyList<LyraTurn>? history)
    {
        var context = new List<string>();
        if (bookContext is not null)
        {
            context.Add(bookContext);
        }

        if (webContext is not null)
        {
            context.Add($"Source: Web search\n{webContext}");
        }

        var conversation = history is { Count: > 0 }
            ? $"\n\nConversation so far:\n{string.Join("\n", history.TakeLast(12).Select(turn => $"{(turn.IsUser ? "User" : "Lyra")}: {turn.Content}"))}"
            : string.Empty;
        var contextText = context.Count == 0
            ? "Source: Lyra's local model knowledge"
            : $"Use the following context to answer the user. Do not present web search results or BookNest data as personal certainty.\n\n{string.Join("\n\n", context)}";
        return $"{contextText}{conversation}\n\nUser question: {message.Trim()}";
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        return values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class OllamaRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("system")]
        public string System { get; init; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; init; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; init; }
    }

    private sealed class OllamaResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; init; }
    }
}