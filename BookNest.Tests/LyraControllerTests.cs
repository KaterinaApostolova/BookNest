using BookNest.Controllers;
using BookNest.Models;
using BookNest.Services;
using BookNest.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookNest.Tests;

public class LyraControllerTests
{
    private static LyraController CreateController(Data.BookNestDbContext context, ApplicationUser user, Mock<ILyraService>? lyraServiceMock = null)
    {
        var userManagerMock = TestHelpers.CreateUserManagerMock(user);
        var loggerMock = new Mock<ILogger<LyraController>>();
        lyraServiceMock ??= new Mock<ILyraService>();

        var controller = new LyraController(context, lyraServiceMock.Object, userManagerMock.Object, loggerMock.Object);
        TestHelpers.AttachTempDataAndUser(controller, user.Id);
        return controller;
    }

    [Test]
    public async Task Index_Post_ReturnsView_WhenMessageIsEmpty()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);
        var model = new LyraViewModel { Message = "   " };

        var result = await controller.Index(model, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public async Task Index_Post_CreatesConversationAndMessages_WhenLyraRespondsSuccessfully()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var lyraServiceMock = new Mock<ILyraService>();
        lyraServiceMock
            .Setup(s => s.AskAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IReadOnlyList<LyraTurn>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Here's a great book recommendation!");

        var controller = CreateController(context, user, lyraServiceMock);
        var model = new LyraViewModel { Message = "Recommend me a book" };

        var result = await controller.Index(model, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());

        var conversation = await context.Conversations.FirstOrDefaultAsync(c => c.UserId == user.Id);
        Assert.That(conversation, Is.Not.Null);

        var messages = context.Messages.Where(m => m.ConversationId == conversation!.Id).ToList();
        Assert.That(messages, Has.Count.EqualTo(2));
        Assert.That(messages.Any(m => m.Content == "Recommend me a book"), Is.True);
        Assert.That(messages.Any(m => m.Content == "Here's a great book recommendation!"), Is.True);
    }

    [Test]
    public async Task DeleteConversation_RemovesConversationAndItsMessages()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        context.Users.Add(user);
        var conversation = new Conversation { Subject = "Old chat", UserId = user.Id };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        context.Messages.Add(new Message { ConversationId = conversation.Id, Content = "Hello", UserId = user.Id });
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);

        var result = await controller.DeleteConversation(conversation.Id, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(await context.Conversations.AnyAsync(c => c.Id == conversation.Id), Is.False);
        Assert.That(await context.Messages.AnyAsync(m => m.ConversationId == conversation.Id), Is.False);
    }

    [Test]
    public async Task DeleteConversation_ReturnsNotFound_ForAnotherUsersConversation()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var owner = TestHelpers.CreateUser(id: "owner-1", userName: "owner");
        var otherUser = TestHelpers.CreateUser(id: "other-1", userName: "other");
        context.Users.AddRange(owner, otherUser);
        var conversation = new Conversation { Subject = "Owner's chat", UserId = owner.Id };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        var controller = CreateController(context, otherUser);

        var result = await controller.DeleteConversation(conversation.Id, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
        Assert.That(await context.Conversations.AnyAsync(c => c.Id == conversation.Id), Is.True);
    }
}
