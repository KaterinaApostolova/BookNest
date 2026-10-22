using BookNest.Data;
using BookNest.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Services;

public class RuleBasedReadingAssistantService : IReadingAssistantService
{
    private readonly BookNestDbContext _context;

    public RuleBasedReadingAssistantService(BookNestDbContext context)
    {
        _context = context;
    }

    public async Task<string> GetResponseAsync(string message)
    {
        var request = message.Trim();
        var normalizedRequest = request.ToLowerInvariant();

        if (ContainsAny(normalizedRequest, "hello", "hi", "hey", "good morning", "good evening"))
        {
            return "Hello! I’m Lyra, your BookNest reading assistant. Tell me what kind of story you are in the mood for.";
        }

        if (ContainsAny(normalizedRequest, "help", "booknest", "how do i", "how can i"))
        {
            return "You can browse books, open a book’s details, join its discussion, add it to your Reading Journey, and write reviews. I can also help you search the BookNest collection by title, author, or genre.";
        }

        var books = await _context.Books
            .AsNoTracking()
            .Include(book => book.Authors)
            .Include(book => book.Genres)
            .ToListAsync();

        var matches = books
            .Where(book =>
                book.Title.Contains(request, StringComparison.OrdinalIgnoreCase) ||
                book.Authors.Any(author => author.Name.Contains(request, StringComparison.OrdinalIgnoreCase)) ||
                book.Genres.Any(genre => genre.Name.Contains(request, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(book => book.AverageRating ?? 0)
            .ThenBy(book => book.Title)
            .Take(5)
            .ToList();

        if (matches.Count == 0)
        {
            var words = request
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(word => word.Length >= 4)
                .ToList();

            matches = books
                .Where(book => words.Any(word =>
                    book.Title.Contains(word, StringComparison.OrdinalIgnoreCase) ||
                    book.Authors.Any(author => author.Name.Contains(word, StringComparison.OrdinalIgnoreCase)) ||
                    book.Genres.Any(genre => genre.Name.Contains(word, StringComparison.OrdinalIgnoreCase))))
                .OrderByDescending(book => book.AverageRating ?? 0)
                .ThenBy(book => book.Title)
                .Take(5)
                .ToList();
        }

        if (matches.Count > 0)
        {
            var suggestions = string.Join("; ", matches.Select(book =>
                $"{book.Title} by {string.Join(", ", book.Authors.Select(author => author.Name))}"));

            return $"These BookNest books may be a good match: {suggestions}. Open a title to see its genres, reviews, reading links, and discussion.";
        }

        if (ContainsAny(normalizedRequest, "recommend", "suggest", "read next", "similar", "mood", "genre", "author"))
        {
            var suggestions = await _context.Books
                .AsNoTracking()
                .Include(book => book.Authors)
                .OrderByDescending(book => book.AverageRating ?? 0)
                .ThenBy(book => book.Title)
                .Take(5)
                .ToListAsync();

            if (suggestions.Count > 0)
            {
                return $"For a place to start, try: {string.Join("; ", suggestions.Select(book => book.Title))}. Tell me a genre, author, or book you already like and I’ll narrow it down.";
            }
        }

        return "I’m still learning this collection, but I can help you find books by title, author, or genre. You can also tell me what you liked about a book and we can talk about its plot, characters, or imagine a new fan-fiction scene together.";
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        return values.Any(text.Contains);
    }
}