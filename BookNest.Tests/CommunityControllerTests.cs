using BookNest.Controllers;
using BookNest.Models;
using BookNest.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BookNest.Tests;

public class CommunityControllerTests
{
    private static CommunityController CreateController(Data.BookNestDbContext context, ApplicationUser? currentUser, params string[] roles)
    {
        var userManagerMock = TestHelpers.CreateUserManagerMock(currentUser);
        var controller = new CommunityController(context, userManagerMock.Object, new Mock<BookNest.Services.ITranslationService>().Object);

        if (currentUser is not null)
        {
            TestHelpers.AttachTempDataAndUser(controller, currentUser.Id, roles);
        }
        else
        {
            TestHelpers.AttachTempData(controller);
        }

        return controller;
    }

    [Test]
    public async Task CreateForum_Post_AddsForum_OwnedByCurrentUser()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);
        var model = new ForumViewModel { Name = "Fantasy Lovers", Description = "For fans of fantasy" };

        var result = await controller.CreateForum(model);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var forum = await context.Forums.FirstOrDefaultAsync(f => f.Name == "Fantasy Lovers");
        Assert.That(forum, Is.Not.Null);
        Assert.That(forum!.UserId, Is.EqualTo(user.Id));
    }

    [Test]
    public async Task CreateForum_Post_RejectsDuplicateName()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        context.Users.Add(user);
        context.Forums.Add(new Forum { Name = "Sci-Fi Club", NormalizedName = "SCI-FI CLUB" });
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);
        var model = new ForumViewModel { Name = "Sci-Fi Club" };

        var result = await controller.CreateForum(model);

        Assert.That(result, Is.InstanceOf<ViewResult>());
        Assert.That(controller.ModelState.IsValid, Is.False);
        Assert.That(await context.Forums.CountAsync(f => f.Name == "Sci-Fi Club"), Is.EqualTo(1));
    }

    [Test]
    public async Task CreateTopic_Post_AddsTopic_ToTheGivenForum()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        var forum = new Forum { Name = "General", NormalizedName = "GENERAL" };
        context.Users.Add(user);
        context.Forums.Add(forum);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);
        var model = new ForumTopicViewModel { ForumId = forum.Id, Title = "Welcome!", Content = "Say hi here." };

        var result = await controller.CreateTopic(model);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var topic = await context.ForumTopics.FirstOrDefaultAsync(t => t.ForumId == forum.Id);
        Assert.That(topic, Is.Not.Null);
        Assert.That(topic!.UserId, Is.EqualTo(user.Id));
    }

    [Test]
    public async Task CreatePost_Post_Forbidden_WhenTopicIsLocked()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        var forum = new Forum { Name = "General", NormalizedName = "GENERAL" };
        context.Forums.Add(forum);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var topic = new ForumTopic { ForumId = forum.Id, Title = "Locked", NormalizedTitle = "LOCKED", Content = "Body", IsLocked = true };
        context.ForumTopics.Add(topic);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);
        var model = new ForumPostViewModel { ForumTopicId = topic.Id, Content = "Trying to reply" };

        var result = await controller.CreatePost(model);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
        Assert.That(await context.ForumPosts.AnyAsync(p => p.ForumTopicId == topic.Id), Is.False);
    }

    [Test]
    public async Task CreatePost_Post_AddsReply_WhenTopicIsNotLocked()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        var forum = new Forum { Name = "General", NormalizedName = "GENERAL" };
        context.Forums.Add(forum);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var topic = new ForumTopic { ForumId = forum.Id, Title = "Open", NormalizedTitle = "OPEN", Content = "Body" };
        context.ForumTopics.Add(topic);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);
        var model = new ForumPostViewModel { ForumTopicId = topic.Id, Content = "Happy to join!" };

        var result = await controller.CreatePost(model);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(await context.ForumPosts.AnyAsync(p => p.ForumTopicId == topic.Id), Is.True);
    }

    [Test]
    public async Task DeleteTopic_Forbidden_ForNonOwnerNonAdmin()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var owner = TestHelpers.CreateUser(id: "owner-1", userName: "owner");
        var otherUser = TestHelpers.CreateUser(id: "other-1", userName: "other");
        var forum = new Forum { Name = "General", NormalizedName = "GENERAL" };
        context.Users.AddRange(owner, otherUser);
        context.Forums.Add(forum);
        await context.SaveChangesAsync();

        var topic = new ForumTopic { ForumId = forum.Id, UserId = owner.Id, Title = "Mine", NormalizedTitle = "MINE", Content = "Body" };
        context.ForumTopics.Add(topic);
        await context.SaveChangesAsync();

        var controller = CreateController(context, otherUser);

        var result = await controller.DeleteTopic(topic.Id);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
        Assert.That(await context.ForumTopics.AnyAsync(t => t.Id == topic.Id), Is.True);
    }

    [Test]
    public async Task DeleteTopic_Allowed_ForAdmin_EvenIfNotOwner()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var owner = TestHelpers.CreateUser(id: "owner-1", userName: "owner");
        var admin = TestHelpers.CreateUser(id: "admin-1", userName: "admin");
        var forum = new Forum { Name = "General", NormalizedName = "GENERAL" };
        context.Users.AddRange(owner, admin);
        context.Forums.Add(forum);
        await context.SaveChangesAsync();

        var topic = new ForumTopic { ForumId = forum.Id, UserId = owner.Id, Title = "Mine", NormalizedTitle = "MINE", Content = "Body" };
        context.ForumTopics.Add(topic);
        await context.SaveChangesAsync();

        var controller = CreateController(context, admin, "Admin");

        var result = await controller.DeleteTopic(topic.Id);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(await context.ForumTopics.AnyAsync(t => t.Id == topic.Id), Is.False);
    }

    [Test]
    public async Task ReactToTopic_AddsLike_WhenNoPriorReactionExists()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        var forum = new Forum { Name = "General", NormalizedName = "GENERAL" };
        context.Users.Add(user);
        context.Forums.Add(forum);
        await context.SaveChangesAsync();

        var topic = new ForumTopic { ForumId = forum.Id, Title = "Topic", NormalizedTitle = "TOPIC", Content = "Body" };
        context.ForumTopics.Add(topic);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);

        await controller.ReactToTopic(topic.Id, isLike: true);

        var reaction = await context.ForumReactions.FirstOrDefaultAsync(r => r.ForumTopicId == topic.Id && r.UserId == user.Id);
        Assert.That(reaction, Is.Not.Null);
        Assert.That(reaction!.IsLike, Is.True);
    }

    [Test]
    public async Task ReactToTopic_RemovesReaction_WhenClickingSameReactionAgain()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        var forum = new Forum { Name = "General", NormalizedName = "GENERAL" };
        context.Users.Add(user);
        context.Forums.Add(forum);
        await context.SaveChangesAsync();

        var topic = new ForumTopic { ForumId = forum.Id, Title = "Topic", NormalizedTitle = "TOPIC", Content = "Body" };
        context.ForumTopics.Add(topic);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);

        await controller.ReactToTopic(topic.Id, isLike: true);
        await controller.ReactToTopic(topic.Id, isLike: true);

        var reaction = await context.ForumReactions.FirstOrDefaultAsync(r => r.ForumTopicId == topic.Id && r.UserId == user.Id);
        Assert.That(reaction, Is.Null);
    }

    [Test]
    public async Task ReactToTopic_SwitchesFromLikeToDislike_WhenOppositeReactionClicked()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        var forum = new Forum { Name = "General", NormalizedName = "GENERAL" };
        context.Users.Add(user);
        context.Forums.Add(forum);
        await context.SaveChangesAsync();

        var topic = new ForumTopic { ForumId = forum.Id, Title = "Topic", NormalizedTitle = "TOPIC", Content = "Body" };
        context.ForumTopics.Add(topic);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);

        await controller.ReactToTopic(topic.Id, isLike: true);
        await controller.ReactToTopic(topic.Id, isLike: false);

        var reaction = await context.ForumReactions.FirstOrDefaultAsync(r => r.ForumTopicId == topic.Id && r.UserId == user.Id);
        Assert.That(reaction, Is.Not.Null);
        Assert.That(reaction!.IsLike, Is.False);
    }

    [Test]
    public async Task TogglePinTopic_FlipsIsPinned()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var admin = TestHelpers.CreateUser(id: "admin-1", userName: "admin");
        var forum = new Forum { Name = "General", NormalizedName = "GENERAL" };
        context.Users.Add(admin);
        context.Forums.Add(forum);
        await context.SaveChangesAsync();

        var topic = new ForumTopic { ForumId = forum.Id, Title = "Topic", NormalizedTitle = "TOPIC", Content = "Body", IsPinned = false };
        context.ForumTopics.Add(topic);
        await context.SaveChangesAsync();

        var controller = CreateController(context, admin, "Admin");

        await controller.TogglePinTopic(topic.Id);
        var afterFirstToggle = await context.ForumTopics.AsNoTracking().FirstAsync(t => t.Id == topic.Id);
        Assert.That(afterFirstToggle.IsPinned, Is.True);

        await controller.TogglePinTopic(topic.Id);
        var afterSecondToggle = await context.ForumTopics.AsNoTracking().FirstAsync(t => t.Id == topic.Id);
        Assert.That(afterSecondToggle.IsPinned, Is.False);
    }
}
