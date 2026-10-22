using BookNest.Data;
using BookNest.Models;
using BookNest.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly BookNestDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            BookNestDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var booksCount =
                await _context.Books.CountAsync();

            var usersCount =
                await _context.Users.CountAsync();

            ViewBag.BooksCount = booksCount;
            ViewBag.UsersCount = usersCount;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Users(string? search)
        {
            var users = _userManager.Users
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                users = users.Where(u =>
                    (u.UserName != null &&
                     u.UserName.Contains(search))

                    ||

                    (u.Email != null &&
                     u.Email.Contains(search))

                    ||

                    u.FullName.Contains(search));
            }

            var userList = await users
                .OrderBy(u => u.UserName)
                .ToListAsync();

            var model = new List<AdminUserViewModel>();

            foreach (var user in userList)
            {
                var roles =
                    await _userManager.GetRolesAsync(user);

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

                var reviewsWritten =
                    await _context.Reviews
                        .CountAsync(r =>
                            r.UserId == user.Id);

                model.Add(new AdminUserViewModel
                {
                    Id = user.Id,

                    UserName =
                        user.UserName ?? string.Empty,

                    FullName =
                        user.FullName,

                    Email =
                        user.Email ?? string.Empty,

                    MemberSince =
                        user.CreatedAt,

                    IsProfilePublic =
                        user.IsProfilePublic,

                    IsDeactivated =
                        user.IsDeactivated,

                    IsAdmin =
                        roles.Contains("Admin"),

                    BooksRead =
                        booksRead,

                    CurrentlyReading =
                        currentlyReading,

                    WantToRead =
                        wantToRead,

                    ReviewsWritten =
                        reviewsWritten
                });
            }

            ViewBag.Search = search;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> UserDetails(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user =
                await _userManager.FindByIdAsync(id);

            if (user is null)
            {
                return NotFound();
            }

            var roles =
                await _userManager.GetRolesAsync(user);

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

            var reviewsWritten =
                await _context.Reviews
                    .CountAsync(r =>
                        r.UserId == user.Id);

            var model = new AdminUserDetailsViewModel
            {
                Id = user.Id,

                UserName =
                    user.UserName ?? string.Empty,

                FullName =
                    user.FullName,

                Email =
                    user.Email ?? string.Empty,

                ProfilePictureUrl =
                    user.ProfilePictureUrl,

                Bio =
                    user.Bio,

                FavouriteGenre =
                    user.FavouriteGenre,

                FavouriteBook =
                    user.FavouriteBook,

                MemberSince =
                    user.CreatedAt,

                IsProfilePublic =
                    user.IsProfilePublic,

                IsDeactivated =
                    user.IsDeactivated,

                IsAdmin =
                    roles.Contains("Admin"),

                BooksRead =
                    booksRead,

                CurrentlyReading =
                    currentlyReading,

                WantToRead =
                    wantToRead,

                ReviewsWritten =
                    reviewsWritten
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user =
                await _userManager.FindByIdAsync(id);

            if (user is null)
            {
                return NotFound();
            }

            // Admin accounts cannot be deactivated.
            var roles =
                await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
            {
                TempData["ErrorMessage"] =
                    "Admin accounts cannot be deactivated.";

                return RedirectToAction(
                    nameof(UserDetails),
                    new { id });
            }

            user.IsDeactivated = true;

            var result =
                await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] =
                    "The user could not be deactivated.";

                return RedirectToAction(
                    nameof(UserDetails),
                    new { id });
            }

            TempData["InfoMessage"] =
                "The user has been deactivated successfully.";

            return RedirectToAction(
                nameof(UserDetails),
                new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user =
                await _userManager.FindByIdAsync(id);

            if (user is null)
            {
                return NotFound();
            }

            // Admin accounts do not need activation/deactivation.
            var roles =
                await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
            {
                TempData["ErrorMessage"] =
                    "Admin accounts cannot be managed this way.";

                return RedirectToAction(
                    nameof(UserDetails),
                    new { id });
            }

            user.IsDeactivated = false;

            var result =
                await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] =
                    "The user could not be activated.";

                return RedirectToAction(
                    nameof(UserDetails),
                    new { id });
            }

            TempData["InfoMessage"] =
                "The user has been activated successfully.";

            return RedirectToAction(
                nameof(UserDetails),
                new { id });
        }
    }
}