using BookNest.Data;
using BookNest.Models;
using BookNest.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;

namespace BookNest.Controllers;

[Authorize]
public class ReviewsController : Controller
{
    private readonly BookNestDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReviewsController(
        BookNestDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: Reviews/Create?bookId=5
    [HttpGet]
    public async Task<IActionResult> Create(int bookId)
    {
        var book = await _context.Books
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bookId);

        if (book is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        // One review per user per book
        var alreadyReviewed = await _context.Reviews
            .AnyAsync(r =>
                r.BookId == bookId &&
                r.UserId == user.Id);

        if (alreadyReviewed)
        {
            TempData["InfoMessage"] =
                "You have already reviewed this book.";

            return RedirectToAction("Details", "Books",
                new { id = bookId });
        }

        var model = new ReviewViewModel
        {
            BookId = book.Id,
            BookTitle = book.Title,
            Rating = 0
        };

        return View(model);
    }

    // POST: Reviews/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var book = await _context.Books
            .FirstOrDefaultAsync(b => b.Id == model.BookId);

        if (book is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.BookTitle = book.Title;
            return View(model);
        }

        // Check again on POST
        var alreadyReviewed = await _context.Reviews
            .AnyAsync(r =>
                r.BookId == model.BookId &&
                r.UserId == user.Id);

        if (alreadyReviewed)
        {
            TempData["InfoMessage"] =
                "You have already reviewed this book.";

            return RedirectToAction("Details", "Books",
                new { id = model.BookId });
        }

        var review = new Review
        {
            BookId = book.Id,
            UserId = user.Id,
            Rating = model.Rating,
            Content = model.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);

        await _context.SaveChangesAsync();

        await UpdateAverageRatingAsync(book.Id);

        TempData["InfoMessage"] =
            "Your review has been added.";

        return RedirectToAction("Details", "Books",
            new { id = book.Id });
    }

    // GET: Reviews/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var review = await _context.Reviews
            .Include(r => r.Book)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Only the author can edit
        if (review.UserId != user.Id)
        {
            return Forbid();
        }

        var model = new ReviewViewModel
        {
            Id = review.Id,
            BookId = review.BookId,
            BookTitle = review.Book?.Title ?? string.Empty,
            Rating = review.Rating,
            Content = review.Content,
            UserId = review.UserId,
            Username = review.User?.UserName,
            CreatedAt = review.CreatedAt
        };

        return View(model);
    }

    // POST: Reviews/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ReviewViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var review = await _context.Reviews
            .Include(r => r.Book)
            .FirstOrDefaultAsync(r => r.Id == model.Id);

        if (review is null)
        {
            return NotFound();
        }

        // Only author can edit
        if (review.UserId != user.Id)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            model.BookTitle = review.Book?.Title ?? string.Empty;
            return View(model);
        }

        review.Rating = model.Rating;
        review.Content = model.Content.Trim();

        await _context.SaveChangesAsync();

        await UpdateAverageRatingAsync(review.BookId);

        TempData["InfoMessage"] =
            "Your review has been updated.";

        return RedirectToAction("Details", "Books",
            new { id = review.BookId });
    }

    // GET: Reviews/Delete/5
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await _context.Reviews
            .Include(r => r.Book)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var isAdmin = User.IsInRole("Admin");

        // Author or Admin
        if (review.UserId != user.Id && !isAdmin)
        {
            return Forbid();
        }

        var model = new ReviewViewModel
        {
            Id = review.Id,
            BookId = review.BookId,
            BookTitle = review.Book?.Title ?? string.Empty,
            Rating = review.Rating,
            Content = review.Content,
            UserId = review.UserId,
            Username = review.User?.UserName,
            CreatedAt = review.CreatedAt
        };

        return View(model);
    }

    // POST: Reviews/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var review = await _context.Reviews
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var isAdmin = User.IsInRole("Admin");

        // Author or Admin
        if (review.UserId != user.Id && !isAdmin)
        {
            return Forbid();
        }

        var bookId = review.BookId;

        _context.Reviews.Remove(review);

        await _context.SaveChangesAsync();

        await UpdateAverageRatingAsync(bookId);

        TempData["InfoMessage"] =
            "The review has been deleted.";

        return RedirectToAction("Details", "Books",
            new { id = bookId });
    }

    private async Task UpdateAverageRatingAsync(int bookId)
    {
        var book = await _context.Books
            .FirstOrDefaultAsync(b => b.Id == bookId);

        if (book is null)
        {
            return;
        }

        var ratings = await _context.Reviews
            .Where(r => r.BookId == bookId)
            .Select(r => r.Rating)
            .ToListAsync();

        book.AverageRating = ratings.Count == 0
            ? null
            : Math.Round(ratings.Average(), 2);

        await _context.SaveChangesAsync();
    }
}

