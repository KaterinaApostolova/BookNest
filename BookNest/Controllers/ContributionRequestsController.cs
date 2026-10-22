using BookNest.Models;
using BookNest.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookNest.Data;

namespace BookNest.Controllers
{
    [Authorize]
    public class ContributionRequestsController : Controller
    {
        private readonly BookNestDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ContributionRequestsController(
            BookNestDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> CreateContributionRequest(
            int? id,
            string type)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return RedirectToAction("Login", "Account");
            }

            var validTypes = new[]
            {
                ContributionRequestTypes.ReadingLink,
                ContributionRequestTypes.BookInformation,
                ContributionRequestTypes.NewBook,
                ContributionRequestTypes.GeneralSuggestion,
                ContributionRequestTypes.AdminRequest
            };

            if (!validTypes.Contains(type))
            {
                return BadRequest();
            }

            Book? book = null;

            if (id.HasValue)
            {
                book = await _context.Books
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == id.Value);

                if (book is null)
                {
                    return NotFound();
                }
            }

            // These types require a specific book.
            if ((type == ContributionRequestTypes.ReadingLink ||
                 type == ContributionRequestTypes.BookInformation) &&
                !id.HasValue)
            {
                return BadRequest();
            }

            var model = new ContributionRequestViewModel
            {
                BookId = book?.Id,
                BookTitle = book?.Title ?? string.Empty,
                RequestType = type
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateContributionRequest(
            ContributionRequestViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return RedirectToAction("Login", "Account");
            }

            var validTypes = new[]
            {
                ContributionRequestTypes.ReadingLink,
                ContributionRequestTypes.BookInformation,
                ContributionRequestTypes.NewBook,
                ContributionRequestTypes.GeneralSuggestion,
                ContributionRequestTypes.AdminRequest
            };

            if (!validTypes.Contains(model.RequestType))
            {
                return BadRequest();
            }

            Book? book = null;

            if (model.BookId.HasValue)
            {
                book = await _context.Books
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == model.BookId.Value);

                if (book is null)
                {
                    return NotFound();
                }
            }

            // Reading Link and Book Information
            // must be connected to a book.
            if ((model.RequestType == ContributionRequestTypes.ReadingLink ||
                 model.RequestType == ContributionRequestTypes.BookInformation) &&
                !model.BookId.HasValue)
            {
                return BadRequest();
            }

            if (model.RequestType == ContributionRequestTypes.ReadingLink)
            {
                if (string.IsNullOrWhiteSpace(model.WebsiteName))
                {
                    ModelState.AddModelError(
                        nameof(model.WebsiteName),
                        "Please enter the website name.");
                }

                if (string.IsNullOrWhiteSpace(model.Language))
                {
                    ModelState.AddModelError(
                        nameof(model.Language),
                        "Please enter the language.");
                }

                if (string.IsNullOrWhiteSpace(model.SourceUrl))
                {
                    ModelState.AddModelError(
                        nameof(model.SourceUrl),
                        "Please enter the reading link.");
                }

                if (string.IsNullOrWhiteSpace(model.LinkType))
                {
                    ModelState.AddModelError(
                        nameof(model.LinkType),
                        "Please select the link type.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.BookTitle = book?.Title ?? string.Empty;

                return View(model);
            }

            var request = new ContributionRequest
            {
                BookId = model.BookId,

                UserId = user.Id,

                RequestType = model.RequestType,

                Message = model.Message.Trim(),

                AdditionalInformation =
                    string.IsNullOrWhiteSpace(model.AdditionalInformation)
                        ? null
                        : model.AdditionalInformation.Trim(),

                SourceUrl =
                    string.IsNullOrWhiteSpace(model.SourceUrl)
                        ? null
                        : model.SourceUrl.Trim(),

                WebsiteName =
                    string.IsNullOrWhiteSpace(model.WebsiteName)
                        ? null
                        : model.WebsiteName.Trim(),

                Language =
                    string.IsNullOrWhiteSpace(model.Language)
                        ? null
                        : model.Language.Trim(),

                LinkType =
                    string.IsNullOrWhiteSpace(model.LinkType)
                        ? null
                        : model.LinkType.Trim(),

                Status = ContributionRequestStatuses.Pending,

                CreatedAt = DateTime.UtcNow
            };

            _context.ContributionRequests.Add(request);

            await _context.SaveChangesAsync();

            TempData["InfoMessage"] =
                "Your request has been sent to the moderators.";


            // If request is related to a book,
            // return to that book.
            if (model.BookId.HasValue)
            {
                return RedirectToAction(
                    nameof(BooksController.Details),
                    "Books",
                    new { id = model.BookId.Value });
            }

            // Otherwise return to user's profile.
            return RedirectToAction(
                nameof(ProfileController.Profile),
                "Profile");
        }

        [HttpGet]
        public async Task<IActionResult> MyRequests()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return RedirectToAction("Login", "Account");
            }

            var requests = await _context.ContributionRequests
                .AsNoTracking()
                .Include(r => r.Book)
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return RedirectToAction("Login", "Account");
            }

            var request = await _context.ContributionRequests
                .AsNoTracking()
                .Include(r => r.Book)
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    r.UserId == user.Id);

            if (request is null)
            {
                return NotFound();
            }

            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteContributionRequest(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return RedirectToAction("Login", "Account");
            }

            var request = await _context.ContributionRequests
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    r.UserId == user.Id);

            if (request is null)
            {
                return NotFound();
            }

            // User cannot delete pending requests.
            if (request.Status == ContributionRequestStatuses.Pending)
            {
                TempData["InfoMessage"] =
                    "Pending requests cannot be deleted.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            _context.ContributionRequests.Remove(request);

            await _context.SaveChangesAsync();

            TempData["InfoMessage"] =
                "Your request was deleted.";

            return RedirectToAction(nameof(MyRequests));
        }

        [HttpGet]
        public IActionResult MakeSuggestion()
        {
            var model = new ContributionRequestViewModel
            {
                RequestType =
                    ContributionRequestTypes.GeneralSuggestion
            };

            return View(model);
        }
    }
}