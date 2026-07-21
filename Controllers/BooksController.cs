using BookNest.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Controllers;

public class BooksController : Controller
{
    private readonly BookNestDbContext _context;

    public BooksController(BookNestDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var books = await _context.Books
            .Include(b => b.Author)
            .Include(b => b.Genre)
            .Include(b => b.Language)
            .OrderBy(b => b.Title)
            .ToListAsync();

        return View(books);
    }

    public IActionResult Community() => View();

    [Authorize]
    public IActionResult ReadingJourney() => View();

    [Authorize]
    public IActionResult BookBuddy() => View();
}
