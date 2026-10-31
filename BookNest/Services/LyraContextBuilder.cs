using BookNest.Data;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Services;

/// <summary>
/// Builds the shared system prompt, BookNest data context, and web-search context that every
/// Lyra AI backend (Groq, Ollama, or any future provider) feeds to the underlying model.
/// Keeping this in one place means swapping the AI provider never touches this logic.
/// </summary>
public sealed class LyraContextBuilder
{
    public const string SystemPrompt = "You are Lyra, the reading companion who lives inside BookNest — a warm, book-loving friend, not a search engine or an encyclopedia. You genuinely enjoy talking about books: you get excited about a great plot twist, you have opinions about characters, and you like asking the reader what drew them to a book or what they thought of it. Recommend books, discuss genres, explain books, help users choose what to read, discuss authors, and help users understand BookNest features. Use BookNest information first when it is provided. For BookNest-specific facts — a book's average rating, its community reviews, and a user's Reading Journey status — rely only on the provided BookNest context and never invent these; if BookNest does not have them, mention that lightly in passing, not as an apology. For general knowledge about a book or author (plot, themes, historical background, biography) that BookNest does not provide, answer confidently from your own knowledge — never say a web search failed or apologize for how you got the information. Write like a knowledgeable friend having a real conversation: weave facts into natural sentences and paragraphs, never a labeled list or bullet dump of raw fields, and never repeat section headers like 'BookNest information' back to the user. Keep answers concise but warm, and where it fits naturally, end with a small question or observation that invites the reader to keep talking about the book.";

    private readonly BookNestDbContext _context;

    public LyraContextBuilder(BookNestDbContext context)
    {
        _context = context;
    }

    public async Task<string?> BuildBookContextAsync(string message, int? bookId, string? userId, CancellationToken cancellationToken)
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

    public static bool ShouldSearchWeb(string message, string? bookContext)
    {
        // The book isn't even in BookNest's catalog — the web is the only source we have.
        if (bookContext is null)
        {
            return true;
        }

        // BookNest found the book but has no plot/description on file — the web is needed
        // for almost anything beyond the bare title/author/genres.
        if (bookContext.Contains("Description: Not available"))
        {
            return true;
        }

        // The only case where a web search adds nothing: the question is purely about
        // BookNest's own data (rating, reviews, this user's Reading Journey status) — that's
        // already sitting in bookContext, so skip the search and answer straight from it.
        var bookNestOnlyTerms = new[]
        {
            "rating", "review", "reviews", "reading journey",
            "оценка", "оценки", "ревю", "ревюта", "читателско пътешествие"
        };

        if (ContainsAny(message, bookNestOnlyTerms))
        {
            return false;
        }

        // Everything else — plot, characters, scenes, themes, historical background, and so
        // on — search the web so Lyra grounds the answer instead of guessing from memory.
        return true;
    }

    public static string BuildPrompt(string message, string? bookContext, string? webContext, IReadOnlyList<LyraTurn>? history)
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
            : $"Use the following context to answer the user, but rewrite it into natural conversational prose — never just copy these labeled fields back as a list. Do not present web search results or BookNest data as personal certainty.\n\n{string.Join("\n\n", context)}";
        return $"{contextText}{conversation}\n\nUser question: {message.Trim()}";
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        return values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
    }
}
