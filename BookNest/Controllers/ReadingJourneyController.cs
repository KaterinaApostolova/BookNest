using BookNest.Data;
using BookNest.Models;
using BookNest.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Controllers;

[Authorize]
public class ReadingJourneyController : Controller
{
    private readonly BookNestDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReadingJourneyController(
        BookNestDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToReadingJourney(int bookId)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }

        var bookExists = await _context.Books
            .AnyAsync(b => b.Id == bookId);

        if (!bookExists)
        {
            return NotFound();
        }

        var wantToReadStatus = await _context.ReadingStatuses
            .FirstOrDefaultAsync(s => s.Name == "Want to Read");

        if (wantToReadStatus == null)
        {
            return BadRequest("Reading status was not found.");
        }

        var existingItem = await _context.ReadingLists
            .FirstOrDefaultAsync(r =>
                r.UserId == userId &&
                r.BookId == bookId);

        if (existingItem == null)
        {
            var readingList = new ReadingList
            {
                UserId = userId,
                BookId = bookId,
                ReadingStatusId = wantToReadStatus.Id,
                Name = "Reading Journey"
            };

            _context.ReadingLists.Add(readingList);

            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(ReadingJourney));
    }

    public async Task<IActionResult> ReadingJourney()
    {
        var userId = _userManager.GetUserId(User);

        var readingLists = await _context.ReadingLists
            .Where(r => r.UserId == userId)
            .Include(r => r.Book)
                .ThenInclude(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
            .Include(r => r.Book)
                .ThenInclude(b => b.BookGenres)
                    .ThenInclude(bg => bg.Genre)
            .Include(r => r.ReadingStatus)
            .ToListAsync();

        var model = new ReadingJourneyViewModel
        {
            WantToRead = readingLists
                .Where(r => r.ReadingStatus != null &&
                            r.ReadingStatus.Name == "Want to Read")
                .ToList(),

            CurrentlyReading = readingLists
                .Where(r => r.ReadingStatus != null &&
                            r.ReadingStatus.Name == "Reading")
                .ToList(),

            Finished = readingLists
                .Where(r => r.ReadingStatus != null &&
                            r.ReadingStatus.Name == "Completed")
                .ToList(),

            Statuses = await _context.ReadingStatuses.ToListAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateReadingJourney(int bookId, string statusName)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }

        var readingItem = await _context.ReadingLists
            .FirstOrDefaultAsync(r =>
                r.BookId == bookId &&
                r.UserId == userId);

        if (readingItem == null)
        {
            return NotFound();
        }

        var status = await _context.ReadingStatuses
            .FirstOrDefaultAsync(s => s.Name == statusName);

        if (status == null)
        {
            return BadRequest("Reading status was not found.");
        }

        readingItem.ReadingStatusId = status.Id;

        await _context.SaveChangesAsync();

        if (status.Name == "Completed")
        {
            var book = await _context.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookId);
            TempData["CompletedBookId"] = bookId;
            TempData["CompletedBookTitle"] = book?.Title ?? string.Empty;
        }

        return RedirectToAction(nameof(ReadingJourney));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFromReadingJourney(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }

        var readingItem = await _context.ReadingLists
            .FirstOrDefaultAsync(r =>
                r.Id == id &&
                r.UserId == userId);

        if (readingItem == null)
        {
            return NotFound();
        }

        _context.ReadingLists.Remove(readingItem);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(ReadingJourney));
    }
}