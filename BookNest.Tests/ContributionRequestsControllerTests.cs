using BookNest.Controllers;
using BookNest.Models;
using BookNest.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Tests;

public class ContributionRequestsControllerTests
{
    private static ContributionRequestsController CreateController(Data.BookNestDbContext context, ApplicationUser user)
    {
        var userManagerMock = TestHelpers.CreateUserManagerMock(user);
        var controller = new ContributionRequestsController(context, userManagerMock.Object);
        TestHelpers.AttachTempDataAndUser(controller, user.Id);
        return controller;
    }

    [Test]
    public async Task CreateContributionRequest_Post_AddsPendingGeneralSuggestion()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);
        var model = new ContributionRequestViewModel
        {
            RequestType = ContributionRequestTypes.GeneralSuggestion,
            Message = "Please add dark mode."
        };

        var result = await controller.CreateContributionRequest(model);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var saved = await context.ContributionRequests.FirstOrDefaultAsync(r => r.UserId == user.Id);
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.Status, Is.EqualTo(ContributionRequestStatuses.Pending));
    }

    [Test]
    public async Task CreateContributionRequest_Post_RejectsUnknownRequestType()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);
        var model = new ContributionRequestViewModel
        {
            RequestType = "NotARealType",
            Message = "Something"
        };

        var result = await controller.CreateContributionRequest(model);

        Assert.That(result, Is.InstanceOf<BadRequestResult>());
    }

    [Test]
    public async Task DeleteContributionRequest_BlockedWhilePending()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new ContributionRequest
        {
            UserId = user.Id,
            RequestType = ContributionRequestTypes.GeneralSuggestion,
            Message = "Pending idea",
            Status = ContributionRequestStatuses.Pending,
            CreatedAt = DateTime.UtcNow
        };
        context.ContributionRequests.Add(request);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);

        var result = await controller.DeleteContributionRequest(request.Id);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(await context.ContributionRequests.AnyAsync(r => r.Id == request.Id), Is.True);
    }

    [Test]
    public async Task DeleteContributionRequest_Succeeds_WhenNotPending()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new ContributionRequest
        {
            UserId = user.Id,
            RequestType = ContributionRequestTypes.GeneralSuggestion,
            Message = "Resolved idea",
            Status = ContributionRequestStatuses.Resolved,
            CreatedAt = DateTime.UtcNow
        };
        context.ContributionRequests.Add(request);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);

        var result = await controller.DeleteContributionRequest(request.Id);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(await context.ContributionRequests.AnyAsync(r => r.Id == request.Id), Is.False);
    }

    [Test]
    public async Task DeleteContributionRequest_ReturnsNotFound_ForAnotherUsersRequest()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var owner = TestHelpers.CreateUser(id: "owner-1", userName: "owner");
        var otherUser = TestHelpers.CreateUser(id: "other-1", userName: "other");
        context.Users.AddRange(owner, otherUser);

        var request = new ContributionRequest
        {
            UserId = owner.Id,
            RequestType = ContributionRequestTypes.GeneralSuggestion,
            Message = "Owner's idea",
            Status = ContributionRequestStatuses.Resolved,
            CreatedAt = DateTime.UtcNow
        };
        context.ContributionRequests.Add(request);
        await context.SaveChangesAsync();

        var controller = CreateController(context, otherUser);

        var result = await controller.DeleteContributionRequest(request.Id);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
        Assert.That(await context.ContributionRequests.AnyAsync(r => r.Id == request.Id), Is.True);
    }
}
