using BookNest.Data;
using BookNest.Models;
using BookNest.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;

namespace BookNest.Controllers;

[Authorize(Roles = "Admin")]
public class ReadingLinksController : Controller
{
    private readonly BookNestDbContext _context;

    public ReadingLinksController(
        BookNestDbContext context)
    {
        _context = context;
    }
    public async Task<IActionResult> AddReadingLink(int bookId)
    {
        var book = await _context.Books
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bookId);

        if (book is null)
        {
            return NotFound();
        }

        var model = new ReadingLinkViewModel
        {
            BookId = book.Id
        };

        ViewBag.BookTitle = book.Title;

        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReadingLink(ReadingLinkViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var book = await _context.Books
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == model.BookId);

            ViewBag.BookTitle = book?.Title;

            return View(model);
        }

        var bookExists = await _context.Books
            .AnyAsync(b => b.Id == model.BookId);

        if (!bookExists)
        {
            return NotFound();
        }

        var readingLink = new ReadingLink
        {
            BookId = model.BookId,
            WebsiteName = model.WebsiteName.Trim(),
            Language = model.Language.Trim(),
            Url = model.Url.Trim(),
            LinkType = model.LinkType.Trim()
        };

        _context.ReadingLinks.Add(readingLink);

        await _context.SaveChangesAsync();

        TempData["InfoMessage"] = "Reading link was added successfully.";

        return RedirectToAction(nameof(BooksController.Details),"Books", new { id = model.BookId });
    }

    public async Task<IActionResult> EditReadingLink(int id)
    {
        var link = await _context.ReadingLinks
            .Include(r => r.Book)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (link is null)
        {
            return NotFound();
        }

        var model = new ReadingLinkViewModel
        {
            Id = link.Id,
            BookId = link.BookId,
            WebsiteName = link.WebsiteName,
            Language = link.Language,
            Url = link.Url,
            LinkType = link.LinkType
        };

        ViewBag.BookTitle = link.Book?.Title;

        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditReadingLink(
    int id,
    ReadingLinkViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.BookTitle = await _context.ReadingLinks
                .Where(r => r.Id == id)
                .Select(r => r.Book!.Title)
                .FirstOrDefaultAsync();

            return View(model);
        }

        var link = await _context.ReadingLinks
            .FirstOrDefaultAsync(r => r.Id == id);

        if (link is null)
        {
            return NotFound();
        }

        link.WebsiteName = model.WebsiteName.Trim();
        link.Language = model.Language.Trim();
        link.Url = model.Url.Trim();
        link.LinkType = model.LinkType.Trim();

        await _context.SaveChangesAsync();

        TempData["InfoMessage"] = "Reading link was updated successfully.";

        return RedirectToAction(nameof(BooksController.Details),"Books", new { id = link.BookId });
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReadingLink(int id)
    {
        var link = await _context.ReadingLinks
            .FirstOrDefaultAsync(r => r.Id == id);

        if (link is null)
        {
            return NotFound();
        }

        var bookId = link.BookId;

        _context.ReadingLinks.Remove(link);

        await _context.SaveChangesAsync();

        TempData["InfoMessage"] = "Reading link was deleted successfully.";

        return RedirectToAction(nameof(BooksController.Details),"Books", new { id = bookId });
    }
}
