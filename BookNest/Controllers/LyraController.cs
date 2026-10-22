using BookNest.Data;
using BookNest.Models;
using BookNest.Services;
using BookNest.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Controllers;

[Authorize]
public class LyraController : Controller
{
    private readonly BookNestDbContext _context;
    private readonly ILyraService _lyraService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<LyraController> _logger;

    public LyraController(BookNestDbContext context, ILyraService lyraService, UserManager<ApplicationUser> userManager, ILogger<LyraController> logger)
    {
        _context = context;
        _lyraService = lyraService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? id, int? bookId)
    {
        var userId = _userManager.GetUserId(User)!;
        return View(await BuildViewModelAsync(userId, id, bookId));
    }

    [HttpGet]
    public async Task<IActionResult> BookDiscussion(int bookId, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User)!;
        var book = await _context.Books
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == bookId, cancellationToken);
        if (book is null)
        {
            return NotFound();
        }

        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(item => item.UserId == userId && item.BookId == bookId && item.BookDiscussionId == null, cancellationToken);
        if (conversation is null)
        {
            conversation = new Conversation
            {
                Subject = book.Title,
                UserId = userId,
                BookId = book.Id
            };
            _context.Conversations.Add(conversation);
        }
        else if (conversation.Subject != book.Title)
        {
            conversation.Subject = book.Title;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index), new { id = conversation.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LyraViewModel model, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User)!;
        var message = model.Message.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            if (IsAjaxRequest())
            {
                return BadRequest(new { error = "Please enter a message for Lyra." });
            }

            await PopulateConversationsAsync(model, userId);
            return View(model);
        }

        model.Message = message;

        try
        {
            var conversation = model.ConversationId.HasValue
                ? await _context.Conversations.FirstOrDefaultAsync(c => c.Id == model.ConversationId && c.UserId == userId, cancellationToken)
                : null;

            var conversationBookTitle = model.BookId.HasValue
                ? await _context.Books
                    .Where(book => book.Id == model.BookId.Value)
                    .Select(book => book.Title)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

            if (conversation is null)
            {
                var subject = conversationBookTitle ?? message;
                conversation = new Conversation
                {
                    Subject = subject.Length > 150 ? subject[..150] : subject,
                    UserId = userId,
                    BookId = model.BookId
                };
                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var history = await _context.Messages
                .Where(message => message.ConversationId == conversation.Id)
                .OrderBy(message => message.SentAt)
                .Select(message => new LyraTurn(message.UserId == userId, message.Content))
                .ToListAsync(cancellationToken);

            _context.Messages.Add(new Message
            {
                ConversationId = conversation.Id,
                UserId = userId,
                Content = message
            });
            await _context.SaveChangesAsync(cancellationToken);

            var response = await _lyraService.AskAsync(message, conversation.BookId, history, userId, cancellationToken);
            _context.Messages.Add(new Message { ConversationId = conversation.Id, Content = response });
            await _context.SaveChangesAsync(cancellationToken);

            if (IsAjaxRequest())
            {
                return Json(new
                {
                    conversationId = conversation.Id,
                    subject = conversation.Subject,
                    userMessage = message,
                    response
                });
            }

            return RedirectToAction(nameof(Index), new { id = conversation.Id });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (!IsAjaxRequest())
            {
                throw;
            }

            return StatusCode(StatusCodes.Status408RequestTimeout, new { error = "Lyra is currently unavailable. Please try again." });
        }
        catch (Exception exception)
        {
            if (!IsAjaxRequest())
            {
                throw;
            }

            _logger.LogError(exception, "Lyra request failed for user {UserId}.", userId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Lyra is currently unavailable. Please make sure the local AI service is running and try again." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult NewChat() => RedirectToAction(nameof(Index));

    private async Task<LyraViewModel> BuildViewModelAsync(string userId, int? conversationId, int? bookId)
    {
        var model = new LyraViewModel { ConversationId = conversationId, BookId = bookId };
        await PopulateConversationsAsync(model, userId);

        if (!conversationId.HasValue)
        {
            return model;
        }

        var conversation = await _context.Conversations
            .AsNoTracking()
            .Include(item => item.Messages)
            .FirstOrDefaultAsync(item => item.Id == conversationId && item.UserId == userId);
        if (conversation is null)
        {
            model.ConversationId = null;
            return model;
        }

        model.BookId = conversation.BookId;
        model.Messages = conversation.Messages
            .OrderBy(message => message.SentAt)
            .Select(message => new LyraMessageViewModel
            {
                Content = message.Content,
                SentAt = message.SentAt,
                IsUser = message.UserId == userId
            }).ToList();
        return model;
    }

    private async Task PopulateConversationsAsync(LyraViewModel model, string userId)
    {
        model.Conversations = await _context.Conversations
            .Where(conversation => conversation.UserId == userId && conversation.BookDiscussionId == null)
            .OrderByDescending(conversation => conversation.Messages.Max(message => (DateTime?)message.SentAt) ?? conversation.CreatedAt)
            .Select(conversation => new LyraConversationViewModel
            {
                Id = conversation.Id,
                Subject = conversation.Subject,
                UpdatedAt = conversation.Messages.Max(message => (DateTime?)message.SentAt) ?? conversation.CreatedAt
            }).ToListAsync();
    }

    private bool IsAjaxRequest() => Request.Headers.XRequestedWith == "XMLHttpRequest";
}