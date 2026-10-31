using BookNest.Controllers;
using BookNest.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Tests;

public class ModerationControllerTests
{
    private static ModerationController CreateController(Data.BookNestDbContext context, ApplicationUser admin)
    {
        var userManagerMock = TestHelpers.CreateUserManagerMock(admin);
        var roleManagerMock = TestHelpers.CreateRoleManagerMock();
        var controller = new ModerationController(context, userManagerMock.Object, roleManagerMock.Object);
        TestHelpers.AttachTempDataAndUser(controller, admin.Id, "Admin");
        return controller;
    }

    [Test]
    public async Task ApproveContributionRequest_ApprovesGeneralSuggestion()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var admin = TestHelpers.CreateUser(id: "admin-1", userName: "admin");
        var submitter = TestHelpers.CreateUser(id: "user-1", userName: "submitter");
        context.Users.AddRange(admin, submitter);

        var request = new ContributionRequest
        {
            UserId = submitter.Id,
            RequestType = ContributionRequestTypes.GeneralSuggestion,
            Message = "Add a dark mode.",
            Status = ContributionRequestStatuses.Pending,
            CreatedAt = DateTime.UtcNow
        };
        context.ContributionRequests.Add(request);
        await context.SaveChangesAsync();

        var controller = CreateController(context, admin);

        var result = await controller.ApproveContributionRequest(request.Id);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var updated = await context.ContributionRequests.FirstAsync(r => r.Id == request.Id);
        Assert.That(updated.Status, Is.EqualTo(ContributionRequestStatuses.Approved));
    }

    [Test]
    public async Task ApproveContributionRequest_DoesNothing_WhenAlreadyProcessed()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var admin = TestHelpers.CreateUser(id: "admin-1", userName: "admin");
        var submitter = TestHelpers.CreateUser(id: "user-1", userName: "submitter");
        context.Users.AddRange(admin, submitter);

        var request = new ContributionRequest
        {
            UserId = submitter.Id,
            RequestType = ContributionRequestTypes.GeneralSuggestion,
            Message = "Already handled.",
            Status = ContributionRequestStatuses.Resolved,
            CreatedAt = DateTime.UtcNow
        };
        context.ContributionRequests.Add(request);
        await context.SaveChangesAsync();

        var controller = CreateController(context, admin);

        var result = await controller.ApproveContributionRequest(request.Id);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var unchanged = await context.ContributionRequests.FirstAsync(r => r.Id == request.Id);
        Assert.That(unchanged.Status, Is.EqualTo(ContributionRequestStatuses.Resolved));
    }

    [Test]
    public async Task ResolveContributionRequest_ResolvesApprovedRequest()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var admin = TestHelpers.CreateUser(id: "admin-1", userName: "admin");
        var submitter = TestHelpers.CreateUser(id: "user-1", userName: "submitter");
        context.Users.AddRange(admin, submitter);

        var request = new ContributionRequest
        {
            UserId = submitter.Id,
            RequestType = ContributionRequestTypes.GeneralSuggestion,
            Message = "Approved idea.",
            Status = ContributionRequestStatuses.Approved,
            CreatedAt = DateTime.UtcNow
        };
        context.ContributionRequests.Add(request);
        await context.SaveChangesAsync();

        var controller = CreateController(context, admin);

        var result = await controller.ResolveContributionRequest(request.Id);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var updated = await context.ContributionRequests.FirstAsync(r => r.Id == request.Id);
        Assert.That(updated.Status, Is.EqualTo(ContributionRequestStatuses.Resolved));
    }

    [Test]
    public async Task ResolveContributionRequest_Rejected_WhenNotApprovedYet()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var admin = TestHelpers.CreateUser(id: "admin-1", userName: "admin");
        var submitter = TestHelpers.CreateUser(id: "user-1", userName: "submitter");
        context.Users.AddRange(admin, submitter);

        var request = new ContributionRequest
        {
            UserId = submitter.Id,
            RequestType = ContributionRequestTypes.GeneralSuggestion,
            Message = "Still pending.",
            Status = ContributionRequestStatuses.Pending,
            CreatedAt = DateTime.UtcNow
        };
        context.ContributionRequests.Add(request);
        await context.SaveChangesAsync();

        var controller = CreateController(context, admin);

        var result = await controller.ResolveContributionRequest(request.Id);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var unchanged = await context.ContributionRequests.FirstAsync(r => r.Id == request.Id);
        Assert.That(unchanged.Status, Is.EqualTo(ContributionRequestStatuses.Pending));
    }

    [Test]
    public async Task DeleteContributionRequest_RemovesAnyRequest_RegardlessOfOwner()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var admin = TestHelpers.CreateUser(id: "admin-1", userName: "admin");
        var submitter = TestHelpers.CreateUser(id: "user-1", userName: "submitter");
        context.Users.AddRange(admin, submitter);

        var request = new ContributionRequest
        {
            UserId = submitter.Id,
            RequestType = ContributionRequestTypes.GeneralSuggestion,
            Message = "To remove.",
            Status = ContributionRequestStatuses.Pending,
            CreatedAt = DateTime.UtcNow
        };
        context.ContributionRequests.Add(request);
        await context.SaveChangesAsync();

        var controller = CreateController(context, admin);

        var result = await controller.DeleteContributionRequest(request.Id);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(await context.ContributionRequests.AnyAsync(r => r.Id == request.Id), Is.False);
    }

    [Test]
    public async Task ContributionRequestDetails_ReturnsNotFound_ForMissingRequest()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var admin = TestHelpers.CreateUser(id: "admin-1", userName: "admin");
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var controller = CreateController(context, admin);

        var result = await controller.ContributionRequestDetails(999);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }
}
