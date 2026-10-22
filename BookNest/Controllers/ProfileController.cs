using BookNest.Data;
using BookNest.Models;
using BookNest.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly BookNestDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(
            BookNestDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = await BuildProfileViewModel(user);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new EditProfileViewModel
            {
                FullName = user.FullName,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Bio = user.Bio,
                IsProfilePublic = user.IsProfilePublic,
                MemberSince = user.CreatedAt,
                ProfilePictureUrl = user.ProfilePictureUrl,
                FavouriteGenre = user.FavouriteGenre,
                FavouriteBook = user.FavouriteBook,
                FavouriteQuote = user.FavouriteQuote
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                model.MemberSince = user.CreatedAt;
                return View(model);
            }

            var newUsername = model.Username.Trim();

            if (!string.Equals(
                    user.UserName,
                    newUsername,
                    StringComparison.OrdinalIgnoreCase))
            {
                var existingUser =
                    await _userManager.FindByNameAsync(newUsername);

                if (existingUser is not null &&
                    existingUser.Id != user.Id)
                {
                    ModelState.AddModelError(
                        nameof(model.Username),
                        "This username is already taken.");

                    model.MemberSince = user.CreatedAt;

                    return View(model);
                }

                var usernameResult =
                    await _userManager.SetUserNameAsync(
                        user,
                        newUsername);

                if (!usernameResult.Succeeded)
                {
                    foreach (var error in usernameResult.Errors)
                    {
                        ModelState.AddModelError(
                            nameof(model.Username),
                            error.Description);
                    }

                    model.MemberSince = user.CreatedAt;

                    return View(model);
                }
            }

            user.FullName = model.FullName.Trim();

            user.Bio =
                string.IsNullOrWhiteSpace(model.Bio)
                    ? null
                    : model.Bio.Trim();

            user.IsProfilePublic = model.IsProfilePublic;

            user.ProfilePictureUrl =
                string.IsNullOrWhiteSpace(model.ProfilePictureUrl)
                    ? null
                    : model.ProfilePictureUrl.Trim();
            user.FavouriteGenre =
                string.IsNullOrWhiteSpace(model.FavouriteGenre)
                   ? null
                   : model.FavouriteGenre.Trim();

            user.FavouriteBook =
                string.IsNullOrWhiteSpace(model.FavouriteBook)
                    ? null
                    : model.FavouriteBook.Trim();

            user.FavouriteQuote =
                string.IsNullOrWhiteSpace(model.FavouriteQuote)
                    ? null
                    : model.FavouriteQuote.Trim();

            var newEmail = model.Email.Trim();

            if (!string.Equals(
                    user.Email,
                    newEmail,
                    StringComparison.OrdinalIgnoreCase))
            {
                var existingEmail =
                    await _userManager.FindByEmailAsync(newEmail);

                if (existingEmail is not null &&
                    existingEmail.Id != user.Id)
                {
                    ModelState.AddModelError(
                        nameof(model.Email),
                        "This email is already in use.");

                    model.MemberSince = user.CreatedAt;

                    return View(model);
                }

                user.Email = newEmail;
                user.NormalizedEmail =
                    _userManager.NormalizeEmail(newEmail);
            }

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                if (string.IsNullOrWhiteSpace(model.CurrentPassword))
                {
                    ModelState.AddModelError(
                        nameof(model.CurrentPassword),
                        "Please enter your current password.");

                    model.MemberSince = user.CreatedAt;

                    return View(model);
                }

                var passwordResult =
                    await _userManager.ChangePasswordAsync(
                        user,
                        model.CurrentPassword,
                        model.Password);

                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }

                    model.MemberSince = user.CreatedAt;

                    return View(model);
                }
            }

            var updateResult =
                await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                model.MemberSince = user.CreatedAt;

                return View(model);
            }

            TempData["InfoMessage"] =
                "Your profile has been updated successfully.";

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFavouriteQuote()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return RedirectToAction("Login", "Account");
            }

            user.FavouriteQuote = null;
            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(Profile));
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Public(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return NotFound();
            }

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u =>
                    u.UserName != null &&
                    u.UserName.ToLower() == username.ToLower());

            if (user is null)
            {
                return NotFound();
            }

            if (!user.IsProfilePublic)
            {
                return View("AnonymousProfile");
            }

            var reviews = await _context.Reviews
                .AsNoTracking()
                .Include(r => r.Book)
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new PublicReviewViewModel
                {
                    BookId = r.BookId,

                    BookTitle = r.Book != null
                        ? r.Book.Title
                        : "Unknown Book",

                    Content = r.Content,

                    Rating = r.Rating,

                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            var booksInJourney =
                await _context.ReadingLists
                    .CountAsync(r => r.UserId == user.Id);

            var booksRead =
                await _context.ReadingLists
                    .CountAsync(r =>
                        r.UserId == user.Id &&
                        r.ReadingStatus.Name == "Completed");




            var model = new PublicProfileViewModel
            {
                DisplayName =
                    user.UserName ?? "Reader",

                FullName =
                    string.IsNullOrWhiteSpace(user.FullName)
                        ? null
                        : user.FullName,

                AvatarInitials =
                    GetInitials(user.FullName),

                Bio = user.Bio,

                ProfilePictureUrl = user.ProfilePictureUrl,

                MemberSince = user.CreatedAt,

                FavouriteGenre = user.FavouriteGenre,

                FavouriteBook = user.FavouriteBook,

                BooksRead = booksRead,

                ReviewsWritten = reviews.Count,

                BooksInJourney = booksInJourney,

                Reviews = reviews

            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> MyPublicProfile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!user.IsProfilePublic)
            {
                return View("AnonymousProfile");
            }

            return RedirectToAction(
                nameof(Public),
                new
                {
                    username = user.UserName
                });
        }

        private async Task<ProfileViewModel> BuildProfileViewModel(
            ApplicationUser user)
        {

            var booksRead =
                await _context.ReadingLists
                    .CountAsync(r =>
                        r.UserId == user.Id &&
                        r.ReadingStatus.Name == "Completed");

            var currentlyReading =
                await _context.ReadingLists
                    .CountAsync(r =>
                        r.UserId == user.Id &&
                        r.ReadingStatus.Name == "Reading");

            var wantToRead =
                await _context.ReadingLists
                    .CountAsync(r =>
                        r.UserId == user.Id &&
                        r.ReadingStatus.Name == "Want To Read");

            var reviews =
                await _context.Reviews
                    .AsNoTracking()
                    .Include(r => r.Book)
                    .Where(r => r.UserId == user.Id)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ProfileReviewViewModel
                    {
                        Id = r.Id,

                        BookId = r.BookId,

                        BookTitle = r.Book != null
                            ? r.Book.Title
                            : "Unknown Book",

                        Rating = r.Rating,

                        Content = r.Content,

                        CreatedAt = r.CreatedAt
                    })
                    .ToListAsync();

            var topicsStarted =
                await _context.ForumTopics
                    .CountAsync(t => t.UserId == user.Id);

            var repliesWritten =
                await _context.ForumPosts
                    .CountAsync(p => p.UserId == user.Id);

            var contributionRequests =
                await _context.ContributionRequests
                    .AsNoTracking()
                    .Include(r => r.Book)
                    .Where(r => r.UserId == user.Id)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ProfileContributionRequestViewModel
                    {
                        Id = r.Id,

                        RequestType = r.RequestType,

                        DisplayType =
                            r.RequestType ==
                                ContributionRequestTypes.ReadingLink
                                ? "Reading Link"

                            : r.RequestType ==
                                ContributionRequestTypes.BookInformation
                                ? "Book Information"

                            : r.RequestType ==
                                ContributionRequestTypes.NewBook
                                ? "New Book"

                            : r.RequestType ==
                                ContributionRequestTypes.GeneralSuggestion
                                ? "General Suggestion"

                            : r.RequestType ==
                                ContributionRequestTypes.AdminRequest
                                ? "Moderator Request"

                            : r.RequestType,

                        BookId = r.BookId,

                        BookTitle =
                            r.Book != null
                                ? r.Book.Title
                                : null,

                        Message = r.Message,

                        Status = r.Status,

                        CreatedAt = r.CreatedAt
                    })
                    .ToListAsync();

            return new ProfileViewModel
            {
                FullName = user.FullName,

                UserName = user.UserName ?? string.Empty,

                Email = user.Email ?? string.Empty,

                AvatarInitials =
                    GetInitials(user.FullName),

                ProfilePictureUrl =
                    user.ProfilePictureUrl,

                Bio = user.Bio,

                MemberSince =
                    user.CreatedAt,

                IsProfilePublic =
                    user.IsProfilePublic,

                FavouriteGenre = user.FavouriteGenre,

                FavouriteBookTitle = user.FavouriteBook,

                FavouriteQuote = user.FavouriteQuote,

                BooksRead = booksRead,

                CurrentlyReading = currentlyReading,

                WantToRead = wantToRead,

                ReviewsWritten =
                    reviews.Count,

                TopicsStarted =
                    topicsStarted,

                RepliesWritten =
                    repliesWritten,

                Reviews =
                    reviews,

                ContributionRequests =
                    contributionRequests
            };
        }


        private static string GetInitials(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return "R";
            }

            var parts = fullName
                .Trim()
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
            {
                return parts[0]
                    .Substring(0, 1)
                    .ToUpper();
            }

            return (
                parts[0].Substring(0, 1) +
                parts[^1].Substring(0, 1)
            ).ToUpper();
        }
    }
}
