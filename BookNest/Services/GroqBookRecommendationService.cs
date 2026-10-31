using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BookNest.Data;
using BookNest.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookNest.Services;

public class GroqBookRecommendationService : IBookRecommendationService
{
    private readonly BookNestDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly LyraOptions _options;
    private readonly ILogger<GroqBookRecommendationService> _logger;

    public GroqBookRecommendationService(
        BookNestDbContext context,
        HttpClient httpClient,
        IOptions<LyraOptions> options,
        ILogger<GroqBookRecommendationService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<BookRecommendation>> GetRecommendationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Groq.ApiKey))
        {
            _logger.LogWarning("Lyra:Groq:ApiKey is not configured; skipping recommendations.");
            return new List<BookRecommendation>();
        }

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return new List<BookRecommendation>();
        }

        var journeyBookIds = await _context.ReadingLists
            .Where(r => r.UserId == userId)
            .Select(r => r.BookId)
            .ToListAsync(cancellationToken);

        var candidateBooks = await _context.Books
            .AsNoTracking()
            .Include(b => b.Authors)
            .Include(b => b.Genres)
            .Where(b => !journeyBookIds.Contains(b.Id))
            .ToListAsync(cancellationToken);

        // Not enough of a catalog left to meaningfully recommend from.
        if (candidateBooks.Count < 3)
        {
            return new List<BookRecommendation>();
        }

        var highlyRatedTitles = await _context.Reviews
            .Where(r => r.UserId == userId && r.Rating >= 4)
            .Include(r => r.Book)
            .Select(r => r.Book!.Title)
            .ToListAsync(cancellationToken);

        var tasteLines = new List<string>();
        if (!string.IsNullOrWhiteSpace(user.FavouriteGenre))
        {
            tasteLines.Add($"Favourite genre: {user.FavouriteGenre}");
        }
        if (!string.IsNullOrWhiteSpace(user.FavouriteBook))
        {
            tasteLines.Add($"Favourite book: {user.FavouriteBook}");
        }
        if (highlyRatedTitles.Count > 0)
        {
            tasteLines.Add($"Rated highly (4-5 stars): {string.Join(", ", highlyRatedTitles.Take(10))}");
        }

        // No taste signal at all yet — nothing meaningful to base a recommendation on.
        if (tasteLines.Count == 0)
        {
            return new List<BookRecommendation>();
        }

        var catalogLines = candidateBooks.Select(b =>
            $"id={b.Id} | \"{b.Title}\" by {string.Join(", ", b.Authors.Select(a => a.Name))} | genres: {string.Join(", ", b.Genres.Select(g => g.Name))}");

        var systemPrompt = "You are a book recommendation engine for BookNest. Given a reader's taste profile and a catalog of available books, pick up to 3 books from the catalog that best match their taste. Respond with ONLY a JSON array, no other text: [{\"bookId\": <int>, \"reason\": \"<one short sentence, written to the reader as 'you'>\"}]. You must only use bookId values that appear in the provided catalog — never invent one.";
        var userPrompt = $"Reader's taste:\n{string.Join("\n", tasteLines)}\n\nAvailable catalog:\n{string.Join("\n", catalogLines)}";

        try
        {
            var request = new GroqChatRequest
            {
                Model = _options.Groq.Model,
                Temperature = 0.4,
                Messages = new List<GroqChatMessage>
                {
                    new() { Role = "system", Content = systemPrompt },
                    new() { Role = "user", Content = userPrompt }
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
                _logger.LogWarning("Groq returned HTTP status {StatusCode} for recommendations.", response.StatusCode);
                return new List<BookRecommendation>();
            }

            var result = await response.Content.ReadFromJsonAsync<GroqChatResponse>(cancellationToken: cancellationToken);
            var raw = result?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new List<BookRecommendation>();
            }

            var picks = ParsePicks(raw);
            var recommendations = new List<BookRecommendation>();

            foreach (var pick in picks)
            {
                var book = candidateBooks.FirstOrDefault(b => b.Id == pick.BookId);
                // Skip any bookId the model invented that isn't actually in our catalog.
                if (book is not null)
                {
                    recommendations.Add(new BookRecommendation { Book = book, Reason = pick.Reason });
                }
            }

            return recommendations.Take(3).ToList();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            _logger.LogWarning(exception, "Failed to fetch book recommendations.");
            return new List<BookRecommendation>();
        }
    }

    private static List<GroqBookPick> ParsePicks(string raw)
    {
        // The model sometimes wraps JSON in a ```json ... ``` code fence despite instructions — strip it.
        var cleaned = raw.Trim();
        if (cleaned.StartsWith("```"))
        {
            var firstNewline = cleaned.IndexOf('\n');
            var lastFence = cleaned.LastIndexOf("```");
            if (firstNewline >= 0 && lastFence > firstNewline)
            {
                cleaned = cleaned[(firstNewline + 1)..lastFence].Trim();
            }
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<GroqBookPick>>(cleaned) ?? new List<GroqBookPick>();
        }
        catch (System.Text.Json.JsonException)
        {
            return new List<GroqBookPick>();
        }
    }

    private sealed class GroqBookPick
    {
        [JsonPropertyName("bookId")]
        public int BookId { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
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
