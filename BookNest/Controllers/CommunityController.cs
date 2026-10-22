using BookNest.Data;
using BookNest.Models;
using BookNest.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Controllers;

public class CommunityController : Controller
{
    private readonly BookNestDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CommunityController(
        BookNestDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var forums = await _context.Forums
            .AsNoTracking()
            .Include(f => f.Topics)
            .ThenInclude(t => t.Posts)
            .Include(f => f.Book)
            .OrderBy(f => f.Name)
            .ToListAsync();

        return View(forums);
    }

    [Authorize]
    [HttpGet]
    public IActionResult CreateForum()
    {
        return View(new ForumViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateForum(ForumViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(nameof(model.Name), "A forum name is required.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var name = model.Name.Trim();
        var normalizedName = Normalize(name);

        if (await _context.Forums.AnyAsync(f => f.NormalizedName == normalizedName))
        {
            ModelState.AddModelError(nameof(model.Name), "A forum with this name already exists.");
            return View(model);
        }

        _context.Forums.Add(new Forum
        {
            Name = name,
            NormalizedName = normalizedName,
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim()
        });
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteForum(int id)
    {
        var forum = await _context.Forums.FirstOrDefaultAsync(f => f.Id == id);

        if (forum is null)
        {
            return NotFound();
        }

        _context.Forums.Remove(forum);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Forum(int id)
    {
        var forum = await _context.Forums
            .AsNoTracking()
            .Include(f => f.Topics)
            .ThenInclude(t => t.User)
            .Include(f => f.Topics)
            .ThenInclude(t => t.Posts)
            .Include(f => f.Book)
            .FirstOrDefaultAsync(f => f.Id == id);

        return forum is null ? NotFound() : View(forum);
    }

    public async Task<IActionResult> Topic(int id)
    {
        var topic = await _context.ForumTopics
            .AsNoTracking()
            .Include(t => t.Forum)
            .Include(t => t.User)
            .Include(t => t.Posts)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        return topic is null ? NotFound() : View(topic);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> CreateTopic(int forumId)
    {
        var forum = await _context.Forums
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == forumId);

        if (forum is null)
        {
            return NotFound();
        }

        return View(new ForumTopicViewModel
        {
            ForumId = forum.Id,
            ForumName = forum.Name
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTopic(ForumTopicViewModel model)
    {
        var forum = await _context.Forums
            .FirstOrDefaultAsync(f => f.Id == model.ForumId);

        if (forum is null)
        {
            return NotFound();
        }

        var title = model.Title?.Trim() ?? string.Empty;
        var normalizedTitle = Normalize(title);
        if (await _context.ForumTopics.AnyAsync(t => t.ForumId == forum.Id && t.NormalizedTitle == normalizedTitle))
        {
            ModelState.AddModelError(nameof(model.Title), "A topic with this title already exists in this forum.");
        }

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Content))
        {
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                ModelState.AddModelError(nameof(model.Title), "A topic title is required.");
            }
            if (string.IsNullOrWhiteSpace(model.Content))
            {
                ModelState.AddModelError(nameof(model.Content), "Topic content is required.");
            }

            model.ForumName = forum.Name;
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var topic = new ForumTopic
        {
            ForumId = forum.Id,
            UserId = user.Id,
            Title = title,
            NormalizedTitle = normalizedTitle,
            Content = model.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.ForumTopics.Add(topic);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Topic), new { id = topic.Id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePost(ForumPostViewModel model)
    {
        var topicExists = await _context.ForumTopics
            .AnyAsync(t => t.Id == model.ForumTopicId);

        if (!topicExists)
        {
            return NotFound();
        }

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Content))
        {
            if (string.IsNullOrWhiteSpace(model.Content))
            {
                ModelState.AddModelError(nameof(model.Content), "Reply content is required.");
            }

            return await Topic(model.ForumTopicId);
        }

        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        _context.ForumPosts.Add(new ForumPost
        {
            ForumTopicId = model.ForumTopicId,
            UserId = user.Id,
            Content = model.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Topic), new { id = model.ForumTopicId });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> EditTopic(int id)
    {
        var topic = await _context.ForumTopics
            .Include(t => t.Forum)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (topic is null)
        {
            return NotFound();
        }

        if (!CanEdit(topic.UserId))
        {
            return Forbid();
        }

        return View(new ForumTopicViewModel
        {
            Id = topic.Id,
            ForumId = topic.ForumId,
            ForumName = topic.Forum?.Name ?? "Community",
            Title = topic.Title,
            Content = topic.Content ?? string.Empty
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTopic(int id, ForumTopicViewModel model)
    {
        var topic = await _context.ForumTopics
            .Include(t => t.Forum)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (topic is null)
        {
            return NotFound();
        }

        if (!CanEdit(topic.UserId))
        {
            return Forbid();
        }

        var title = model.Title?.Trim() ?? string.Empty;
        if (await _context.ForumTopics.AnyAsync(t =>
                t.ForumId == topic.ForumId &&
                t.Id != topic.Id &&
                t.NormalizedTitle == Normalize(title)))
        {
            ModelState.AddModelError(nameof(model.Title), "A topic with this title already exists in this forum.");
        }

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Content))
        {
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                ModelState.AddModelError(nameof(model.Title), "A topic title is required.");
            }
            if (string.IsNullOrWhiteSpace(model.Content))
            {
                ModelState.AddModelError(nameof(model.Content), "Topic content is required.");
            }

            model.ForumId = topic.ForumId;
            model.ForumName = topic.Forum?.Name ?? "Community";
            model.Id = topic.Id;
            return View(model);
        }

        topic.Title = title;
        topic.NormalizedTitle = Normalize(topic.Title);
        topic.Content = model.Content?.Trim();
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Topic), new { id = topic.Id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTopic(int id)
    {
        var topic = await _context.ForumTopics.FirstOrDefaultAsync(t => t.Id == id);

        if (topic is null)
        {
            return NotFound();
        }

        if (!CanDelete(topic.UserId))
        {
            return Forbid();
        }

        var forumId = topic.ForumId;
        _context.ForumTopics.Remove(topic);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Forum), new { id = forumId });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> EditPost(int id)
    {
        var post = await _context.ForumPosts.FirstOrDefaultAsync(p => p.Id == id);

        if (post is null)
        {
            return NotFound();
        }

        if (!CanEdit(post.UserId))
        {
            return Forbid();
        }

        return View(new ForumPostViewModel
        {
            Id = post.Id,
            ForumTopicId = post.ForumTopicId,
            Content = post.Content
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(int id, ForumPostViewModel model)
    {
        var post = await _context.ForumPosts.FirstOrDefaultAsync(p => p.Id == id);

        if (post is null)
        {
            return NotFound();
        }

        if (!CanEdit(post.UserId))
        {
            return Forbid();
        }

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Content))
        {
            if (string.IsNullOrWhiteSpace(model.Content))
            {
                ModelState.AddModelError(nameof(model.Content), "Reply content is required.");
            }

            model.Id = post.Id;
            model.ForumTopicId = post.ForumTopicId;
            return View(model);
        }

        post.Content = model.Content.Trim();
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Topic), new { id = post.ForumTopicId });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePost(int id)
    {
        var post = await _context.ForumPosts.FirstOrDefaultAsync(p => p.Id == id);

        if (post is null)
        {
            return NotFound();
        }

        if (!CanDelete(post.UserId))
        {
            return Forbid();
        }

        var topicId = post.ForumTopicId;
        _context.ForumPosts.Remove(post);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Topic), new { id = topicId });
    }

    private bool CanEdit(string? contentOwnerId)
    {
        return !string.IsNullOrEmpty(contentOwnerId) &&
            contentOwnerId == _userManager.GetUserId(User);
    }

    private bool CanDelete(string? contentOwnerId)
    {
        return User.IsInRole("Admin") || CanEdit(contentOwnerId);
    }

    [HttpGet]
    public async Task<IActionResult> BookDiscussion(int bookId)
    {
        var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == bookId);
        if (book is null)
        {
            return NotFound();
        }

        var forum = await _context.Forums.FirstOrDefaultAsync(f => f.BookId == bookId);
        if (forum is null)
        {
            var name = $"{book.Title} Discussion";
            forum = new Forum
            {
                Name = name,
                NormalizedName = Normalize(name),
                Description = $"Discuss {book.Title} with other readers.",
                BookId = book.Id
            };
            _context.Forums.Add(forum);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                forum = await _context.Forums.FirstOrDefaultAsync(f => f.BookId == bookId);
                if (forum is null)
                {
                    throw;
                }
            }
        }

        return RedirectToAction(nameof(Forum), new { id = forum.Id });
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}