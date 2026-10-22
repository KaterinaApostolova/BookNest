using BookNest.Data;
using BookNest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Controllers;

[Authorize(Roles = "Admin")]
public class ModerationController : Controller
{
    private readonly BookNestDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public ModerationController(BookNestDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var requests = await _context.ContributionRequests
            .AsNoTracking()
            .Include(r => r.Book)
            .Include(r => r.User)
            .OrderBy(r => r.Status != ContributionRequestStatuses.Pending)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(requests);
    }
   
    public async Task<IActionResult> ContributionRequests(string? status, string? type)
    {
        var query = _context.ContributionRequests
            .Include(r => r.Book)
            .Include(r => r.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(r => r.RequestType == type);
        }

        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        ViewBag.Status = status;
        ViewBag.Type = type;

        return View(requests);
    }
    
    public async Task<IActionResult> ContributionRequestDetails(int id)
    {
        var request = await _context.ContributionRequests
            .Include(r => r.Book)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request is null)
        {
            return NotFound();
        }

        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveContributionRequest(int id)
    {
        var request = await _context.ContributionRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != ContributionRequestStatuses.Pending)
        {
            TempData["InfoMessage"] =
                "This request has already been processed.";

            return RedirectToAction(
                nameof(ContributionRequestDetails),
                new { id });
        }

        // ---------------------------------------------------------
        // READING LINK
        // ---------------------------------------------------------

        if (request.RequestType == ContributionRequestTypes.ReadingLink)
        {
            if (request.BookId is null)
            {
                TempData["InfoMessage"] =
                    "The reading link request has no associated book.";

                return RedirectToAction(
                    nameof(ContributionRequestDetails),
                    new { id });
            }

            var alreadyExists = await _context.ReadingLinks
                .AnyAsync(r =>
                    r.BookId == request.BookId &&
                    r.Url == request.SourceUrl);

            if (!alreadyExists)
            {
                var readingLink = new ReadingLink
                {
                    BookId = request.BookId.Value,
                    WebsiteName = request.WebsiteName ?? string.Empty,
                    Language = request.Language ?? string.Empty,
                    Url = request.SourceUrl ?? string.Empty,
                    LinkType = request.LinkType ?? string.Empty
                };

                _context.ReadingLinks.Add(readingLink);
            }

            request.Status = ContributionRequestStatuses.Resolved;

            await _context.SaveChangesAsync();

            TempData["InfoMessage"] =
                "The reading link was approved and added.";

            return RedirectToAction(
                nameof(ContributionRequestDetails),
                new { id });
        }

        // ---------------------------------------------------------
        // ADMIN REQUEST
        // ---------------------------------------------------------

        if (request.RequestType == ContributionRequestTypes.AdminRequest)
        {
            if (request.User is null)
            {
                TempData["InfoMessage"] =
                    "The user associated with this request could not be found.";

                return RedirectToAction(
                    nameof(ContributionRequestDetails),
                    new { id });
            }

            var isAdmin = await _userManager.IsInRoleAsync(
                request.User,
                "Admin");

            if (!isAdmin)
            {
                var result = await _userManager.AddToRoleAsync(
                    request.User,
                    "Admin");

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        TempData["InfoMessage"] = error.Description;
                    }

                    return RedirectToAction(
                        nameof(ContributionRequestDetails),
                        new { id });
                }
            }

            request.Status = ContributionRequestStatuses.Resolved;

            await _context.SaveChangesAsync();

            TempData["InfoMessage"] =
                "The user has been granted moderator access.";

            return RedirectToAction(
                nameof(ContributionRequestDetails),
                new { id });
        }

        // ---------------------------------------------------------
        // BOOK INFORMATION / NEW BOOK / GENERAL SUGGESTION
        // ---------------------------------------------------------

        request.Status = ContributionRequestStatuses.Approved;

        await _context.SaveChangesAsync();

        TempData["InfoMessage"] =
            "The request was approved. Please apply the requested changes and mark it as resolved.";

        return RedirectToAction(
            nameof(ContributionRequestDetails),
            new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveContributionRequest(int id)
    {
        var request = await _context.ContributionRequests
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != ContributionRequestStatuses.Approved)
        {
            TempData["InfoMessage"] =
                "Only approved requests can be resolved.";

            return RedirectToAction(
                nameof(ContributionRequestDetails),
                new { id });
        }

        request.Status = ContributionRequestStatuses.Resolved;

        await _context.SaveChangesAsync();

        TempData["InfoMessage"] =
            "The request has been marked as resolved.";

        return RedirectToAction(
            nameof(ContributionRequestDetails),
            new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteContributionRequest(int id)
    {
        var request = await _context.ContributionRequests
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request is null)
        {
            return NotFound();
        }

        _context.ContributionRequests.Remove(request);

        await _context.SaveChangesAsync();

        TempData["InfoMessage"] =
            "The request was deleted.";

        return RedirectToAction(nameof(ContributionRequests));
    }
}