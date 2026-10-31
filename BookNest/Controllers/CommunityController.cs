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
    private readonly BookNest.Services.ITranslationService _translationService;

    public CommunityController(
        BookNestDbContext context,
        UserManager<ApplicationUser> userManager,
        BookNest.Services.ITranslationService translationService)
    {
        _context = context;
        _userManager = userManager;
        _translationService = translationService;
    }

    // GET: Community/TranslateTopic/5?targetLanguage=en
    [HttpGet]
    public async Task<IActionResult> TranslateTopic(int id, string targetLanguage, CancellationToken cancellationToken)
    {
        if (targetLanguage != "en" && targetLanguage != "bg")
        {
            return BadRequest();
        }

        var topic = await _context.ForumTopics.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (topic is null)
        {
            return NotFound();
        }

        var languageName = targetLanguage == "en" ? "English" : "Bulgarian";
        var translatedTitle = await _translationService.TranslateAsync(topic.Title, languageName, cancellationToken);
        var translatedContent = string.IsNullOrWhiteSpace(topic.Content)
            ? null
            : await _translationService.TranslateAsync(topic.Content, languageName, cancellationToken);

        if (translatedTitle is null)
        {
            return StatusCode(503, new { error = "Translation is currently unavailable." });
        }

        return Json(new { translatedTitle, translatedContent });
    }

    // GET: Community/TranslatePost/5?targetLanguage=en
    [HttpGet]
    public async Task<IActionResult> TranslatePost(int id, string targetLanguage, CancellationToken cancellationToken)
    {
        if (targetLanguage != "en" && targetLanguage != "bg")
        {
            return BadRequest();
        }

        var post = await _context.ForumPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        var languageName = targetLanguage == "en" ? "English" : "Bulgarian";
        var translated = await _translationService.TranslateAsync(post.Content, languageName, cancellationToken);

        if (translated is null)
        {
            return StatusCode(503, new { error = "Translation is currently unavailable." });
        }

        return Json(new { translated });
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
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            UserId = _userManager.GetUserId(User)
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

    public async Task<IActionResult> Forum(int id, string sort = "newest", int page = 1)
    {
        var forum = await _context.Forums
            .AsNoTracking()
            .Include(f => f.Topics)
            .ThenInclude(t => t.User)
            .Include(f => f.Topics)
            .ThenInclude(t => t.Posts)
            .Include(f => f.Book)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (forum is null)
        {
            return NotFound();
        }

        const int pageSize = 10;

        var allTopics = sort switch
        {
            "active" => forum.Topics.OrderByDescending(t => t.IsPinned)
                .ThenByDescending(t => t.Posts.Any() ? t.Posts.Max(p => p.CreatedAt) : t.CreatedAt)
                .ToList(),
            "replies" => forum.Topics.OrderByDescending(t => t.IsPinned)
                .ThenByDescending(t => t.Posts.Count)
                .ToList(),
            _ => forum.Topics.OrderByDescending(t => t.IsPinned)
                .ThenByDescending(t => t.CreatedAt)
                .ToList()
        };

        var totalPages = Math.Max(1, (int)Math.Ceiling(allTopics.Count / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        var pagedTopics = allTopics.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        await PopulatePostCountsAsync(pagedTopics.Select(t => t.UserId));
        await PopulateTopicReactionsAsync(pagedTopics.Select(t => t.Id));

        return View(new ForumPageViewModel
        {
            Forum = forum,
            Topics = pagedTopics,
            Sort = sort,
            Page = page,
            TotalPages = totalPages
        });
    }

    public async Task<IActionResult> Topic(int id, int page = 1)
    {
        var topic = await _context.ForumTopics
            .Include(t => t.Forum)
            .Include(t => t.User)
            .Include(t => t.Posts)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (topic is null)
        {
            return NotFound();
        }

        topic.ViewCount += 1;
        await _context.SaveChangesAsync();

        const int pageSize = 15;
        var orderedPosts = topic.Posts.OrderBy(p => p.CreatedAt).ToList();
        var totalPages = Math.Max(1, (int)Math.Ceiling(orderedPosts.Count / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        var pagedPosts = orderedPosts.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var userIds = pagedPosts.Select(p => p.UserId).Append(topic.UserId ?? string.Empty);
        await PopulatePostCountsAsync(userIds);
        await PopulateTopicReactionsAsync(new[] { topic.Id });
        await PopulatePostReactionsAsync(pagedPosts.Select(p => p.Id));

        return View(new TopicPageViewModel
        {
            Topic = topic,
            Posts = pagedPosts,
            Page = page,
            TotalPages = totalPages
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePinTopic(int id)
    {
        var topic = await _context.ForumTopics.FirstOrDefaultAsync(t => t.Id == id);
        if (topic is null)
        {
            return NotFound();
        }

        topic.IsPinned = !topic.IsPinned;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Forum), new { id = topic.ForumId });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLockTopic(int id)
    {
        var topic = await _context.ForumTopics.FirstOrDefaultAsync(t => t.Id == id);
        if (topic is null)
        {
            return NotFound();
        }

        topic.IsLocked = !topic.IsLocked;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Topic), new { id = topic.Id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactToTopic(int id, bool isLike)
    {
        var topicExists = await _context.ForumTopics.AnyAsync(t => t.Id == id);
        if (!topicExists)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        await ToggleReactionAsync(userId, isLike, topicId: id, postId: null);

        return RedirectToAction(nameof(Topic), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactToPost(int id, bool isLike)
    {
        var post = await _context.ForumPosts.FirstOrDefaultAsync(p => p.Id == id);
        if (post is null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        await ToggleReactionAsync(userId, isLike, topicId: null, postId: id);

        return RedirectToAction(nameof(Topic), new { id = post.ForumTopicId });
    }

    private async Task ToggleReactionAsync(string userId, bool isLike, int? topicId, int? postId)
    {
        var existing = await _context.ForumReactions.FirstOrDefaultAsync(r =>
            r.UserId == userId &&
            r.ForumTopicId == topicId &&
            r.ForumPostId == postId);

        if (existing is null)
        {
            _context.ForumReactions.Add(new ForumReaction
            {
                UserId = userId,
                ForumTopicId = topicId,
                ForumPostId = postId,
                IsLike = isLike
            });
        }
        else if (existing.IsLike == isLike)
        {
            // Clicking the same reaction again removes it.
            _context.ForumReactions.Remove(existing);
        }
        else
        {
            existing.IsLike = isLike;
        }

        await _context.SaveChangesAsync();
    }

    private async Task PopulatePostCountsAsync(IEnumerable<string?> userIds)
    {
        var ids = userIds.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        var counts = new Dictionary<string, int>();

        foreach (var userId in ids)
        {
            var topicCount = await _context.ForumTopics.CountAsync(t => t.UserId == userId);
            var postCount = await _context.ForumPosts.CountAsync(p => p.UserId == userId);
            counts[userId!] = topicCount + postCount;
        }

        ViewBag.PostCounts = counts;
    }

    private async Task PopulateTopicReactionsAsync(IEnumerable<int> topicIds)
    {
        var ids = topicIds.Distinct().ToList();
        var currentUserId = _userManager.GetUserId(User);
        var summaries = new Dictionary<int, ReactionSummary>();

        foreach (var topicId in ids)
        {
            var reactions = await _context.ForumReactions
                .Where(r => r.ForumTopicId == topicId)
                .ToListAsync();

            summaries[topicId] = new ReactionSummary
            {
                Likes = reactions.Count(r => r.IsLike),
                Dislikes = reactions.Count(r => !r.IsLike),
                MyReaction = reactions.FirstOrDefault(r => r.UserId == currentUserId)?.IsLike
            };
        }

        ViewBag.TopicReactions = summaries;
    }

    private async Task PopulatePostReactionsAsync(IEnumerable<int> postIds)
    {
        var ids = postIds.Distinct().ToList();
        var currentUserId = _userManager.GetUserId(User);
        var summaries = new Dictionary<int, ReactionSummary>();

        foreach (var postId in ids)
        {
            var reactions = await _context.ForumReactions
                .Where(r => r.ForumPostId == postId)
                .ToListAsync();

            summaries[postId] = new ReactionSummary
            {
                Likes = reactions.Count(r => r.IsLike),
                Dislikes = reactions.Count(r => !r.IsLike),
                MyReaction = reactions.FirstOrDefault(r => r.UserId == currentUserId)?.IsLike
            };
        }

        ViewBag.PostReactions = summaries;
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
            CreatedAt = DateTime.UtcNow,
            IsAnonymous = model.IsAnonymous
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
        var topic = await _context.ForumTopics
            .FirstOrDefaultAsync(t => t.Id == model.ForumTopicId);

        if (topic is null)
        {
            return NotFound();
        }

        if (topic.IsLocked)
        {
            return Forbid();
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
            CreatedAt = DateTime.UtcNow,
            IsAnonymous = model.IsAnonymous
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
            Content = topic.Content ?? string.Empty,
            IsAnonymous = topic.IsAnonymous
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
        topic.IsAnonymous = model.IsAnonymous;
        topic.EditedAt = DateTime.UtcNow;
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

        var postIds = await _context.ForumPosts
            .Where(p => p.ForumTopicId == topic.Id)
            .Select(p => p.Id)
            .ToListAsync();

        var reactionsToRemove = await _context.ForumReactions
            .Where(r => r.ForumTopicId == topic.Id || (r.ForumPostId != null && postIds.Contains(r.ForumPostId.Value)))
            .ToListAsync();
        _context.ForumReactions.RemoveRange(reactionsToRemove);

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
            Content = post.Content,
            IsAnonymous = post.IsAnonymous
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
        post.IsAnonymous = model.IsAnonymous;
        post.EditedAt = DateTime.UtcNow;
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

        var postReactions = await _context.ForumReactions
            .Where(r => r.ForumPostId == post.Id)
            .ToListAsync();
        _context.ForumReactions.RemoveRange(postReactions);

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