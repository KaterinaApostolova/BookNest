using BookNest.Data;
using BookNest.Interfaces;
using BookNest.Models;
using BookNest.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Controllers;

public class BooksController : Controller
{
    private readonly BookNestDbContext _context;

    public BooksController(
        BookNestDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? searchTerm, int? genreId, string? sortOrder)
    {
        var booksQuery = _context.Books
            .AsNoTracking()
            .Include(b => b.Authors)
            .Include(b => b.Genres)
            .Include(b => b.ReadingLinks)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalized = searchTerm.Trim();
            booksQuery = booksQuery.Where(b =>
                b.Title.Contains(normalized) ||
                b.Authors.Any(a => a.Name.Contains(normalized)));
        }

        if (genreId.HasValue)
        {
            booksQuery = booksQuery.Where(b => b.Genres.Any(g => g.Id == genreId.Value));
        }

        booksQuery = sortOrder switch
        {
            "z-a" => booksQuery.OrderByDescending(b => b.Title),
            "highest-rated" => booksQuery.OrderByDescending(b => b.AverageRating ?? 0).ThenBy(b => b.Title),
            "newest-first" => booksQuery.OrderByDescending(b => b.PublicationYear ?? 0).ThenBy(b => b.Title),
            _ => booksQuery.OrderBy(b => b.Title)
        };

        var books = await booksQuery.ToListAsync();
        var genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();

        ViewBag.Genres = genres;
        ViewBag.SearchTerm = searchTerm;
        ViewBag.GenreId = genreId;
        ViewBag.SortOrder = sortOrder ?? "asc";

        return View(books);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var book = await _context.Books
                  .AsNoTracking()
                  .Include(b => b.Authors)
                  .Include(b => b.Genres)
                  .Include(b => b.ReadingLinks)
                  .Include(b => b.Reviews)
                  .ThenInclude(r => r.User)
                  .FirstOrDefaultAsync(b => b.Id == id);

        if (book is null)
        {
            return NotFound();
        }

        ViewBag.SimilarBooks = await GetSimilarBooksAsync(book);

        ViewBag.ReadingStatuses = await _context.ReadingStatuses
        .OrderBy(s => s.Id)
        .ToListAsync(); 

        return View(book);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        return View(new BookFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(BookFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Split authors entered in the text field by commas
        var authorNames = model.Authors
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim())
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Split genres entered in the text field by commas
        var genreNames = model.Genres
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(g => g.Trim())
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!authorNames.Any())
        {
            ModelState.AddModelError(nameof(model.Authors),
                "Please enter at least one author.");

            return View(model);
        }

        if (!genreNames.Any())
        {
            ModelState.AddModelError(nameof(model.Genres),
                "Please enter at least one genre.");

            return View(model);
        }

        var authors = new List<Author>();

        foreach (var name in authorNames)
        {
            var author = await _context.Authors
                .FirstOrDefaultAsync(a => a.Name.ToLower() == name.ToLower());

            if (author == null)
            {
                author = new Author
                {
                    Name = name
                };

                _context.Authors.Add(author);
            }

            authors.Add(author);
        }

        var genres = new List<Genre>();

        foreach (var name in genreNames)
        {
            var genre = await _context.Genres
                .FirstOrDefaultAsync(g => g.Name.ToLower() == name.ToLower());

            if (genre == null)
            {
                genre = new Genre
                {
                    Name = name
                };

                _context.Genres.Add(genre);
            }

            genres.Add(genre);
        }

        var book = new Book
        {
            Title = model.Title,
            Description = model.Description,
            PublicationYear = model.PublicationYear,
            CoverImageUrl = model.CoverImageUrl,
            AverageRating = model.AverageRating,
            Authors = authors,
            Genres = genres
        };

        _context.Books.Add(book);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var book = await _context.Books
            .Include(b => b.Authors)
            .Include(b => b.Genres)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (book is null)
        {
            return NotFound();
        }

        var model = new BookFormViewModel
        {
            Id = book.Id,
            Title = book.Title,
            Description = book.Description,
            PublicationYear = book.PublicationYear,
            CoverImageUrl = book.CoverImageUrl,
            AverageRating = book.AverageRating,
            Authors = string.Join(", ", book.Authors.Select(a => a.Name)),
            Genres = string.Join(", ", book.Genres.Select(g => g.Name))
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, BookFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingBook = await _context.Books
            .Include(b => b.Authors)
            .Include(b => b.Genres)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (existingBook is null)
        {
            return NotFound();
        }

        // Split authors from the text field
        var authorNames = model.Authors
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim())
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Split genres from the text field
        var genreNames = model.Genres
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(g => g.Trim())
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!authorNames.Any())
        {
            ModelState.AddModelError(nameof(model.Authors),
                "Please enter at least one author.");

            return View(model);
        }

        if (!genreNames.Any())
        {
            ModelState.AddModelError(nameof(model.Genres),
                "Please enter at least one genre.");

            return View(model);
        }

        var authors = new List<Author>();

        foreach (var name in authorNames)
        {
            var author = await _context.Authors
                .FirstOrDefaultAsync(a => a.Name.ToLower() == name.ToLower());

            if (author == null)
            {
                author = new Author
                {
                    Name = name
                };

                _context.Authors.Add(author);
            }

            authors.Add(author);
        }

        var genres = new List<Genre>();

        foreach (var name in genreNames)
        {
            var genre = await _context.Genres
                .FirstOrDefaultAsync(g => g.Name.ToLower() == name.ToLower());

            if (genre == null)
            {
                genre = new Genre
                {
                    Name = name
                };

                _context.Genres.Add(genre);
            }

            genres.Add(genre);
        }

        existingBook.Title = model.Title;
        existingBook.Description = model.Description;
        existingBook.PublicationYear = model.PublicationYear;
        existingBook.CoverImageUrl = model.CoverImageUrl;
        existingBook.AverageRating = model.AverageRating;

        // Replace the existing Many-to-Many relationships
        existingBook.Authors.Clear();
        existingBook.Genres.Clear();

        foreach (var author in authors)
        {
            existingBook.Authors.Add(author);
        }

        foreach (var genre in genres)
        {
            existingBook.Genres.Add(genre);
        }

        await _context.SaveChangesAsync();

        TempData["InfoMessage"] = $"Book '{model.Title}' was updated.";

        return RedirectToAction(nameof(Details), new { id = model.Id });
    }
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var book = await _context.Books
            .Include(b => b.Authors)
            .Include(b => b.Genres)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (book is null)
        {
            return NotFound();
        }

        return View(book);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book is null)
        {
            return NotFound();
        }

        _context.Books.Remove(book);
        await _context.SaveChangesAsync();
        TempData["InfoMessage"] = $"Book '{book.Title}' was deleted.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpGet]
    public IActionResult BookBuddy()
    {
        return RedirectToAction("Index", "Lyra");
    }

    private async Task<List<Book>> GetSimilarBooksAsync(Book currentBook)
    {
        var allBooks = await _context.Books
            .AsNoTracking()
            .Include(b => b.Authors)
            .Include(b => b.Genres)
            .Where(b => b.Id != currentBook.Id)
            .ToListAsync();

        var currentGenreIds = currentBook.Genres
            .Select(g => g.Id)
            .ToHashSet();

        var currentAuthorIds = currentBook.Authors
            .Select(a => a.Id)
            .ToHashSet();

        var similarBooks = allBooks
            .Select(book =>
            {
                var commonGenres = book.Genres
                    .Count(g => currentGenreIds.Contains(g.Id));

                var commonAuthors = book.Authors
                    .Count(a => currentAuthorIds.Contains(a.Id));

                var score = (commonAuthors * 10) + (commonGenres * 3);

                return new
                {
                    Book = book,
                    Score = score
                };
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Book.AverageRating ?? 0)
            .ThenBy(x => x.Book.Title)
            .Take(5)
            .Select(x => x.Book)
            .ToList();

        return similarBooks;
    }

}
