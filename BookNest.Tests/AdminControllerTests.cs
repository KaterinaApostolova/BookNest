using BookNest.Controllers;
using BookNest.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BookNest.Tests;

public class AdminControllerTests
{
    private static AdminController CreateController(
        Data.BookNestDbContext context,
        ApplicationUser admin,
        out Mock<UserManager<ApplicationUser>> userManagerMock)
    {
        userManagerMock = TestHelpers.CreateUserManagerMock(admin);
        userManagerMock.Setup(m => m.Users).Returns(context.Users);
        userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string>());
        var controller = new AdminController(context, userManagerMock.Object);
        TestHelpers.AttachTempDataAndUser(controller, admin.Id, "Admin");
        return controller;
    }

    [Test]
    public async Task DeactivateUser_DeactivatesRegularUser()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var admin = TestHelpers.CreateUser(id: "admin-1", userName: "admin");
        var target = TestHelpers.CreateUser(id: "target-1", userName: "target");
        context.Users.AddRange(admin, target);
        await context.SaveChangesAsync();

        var controller = CreateController(context, admin, out var userManagerMock);
        userManagerMock.Setup(m => m.FindByIdAsync(target.Id)).ReturnsAsync(target);
        userManagerMock.Setup(m => m.GetRolesAsync(target)).ReturnsAsync(new List<string> { "User" });
        userManagerMock.Setup(m => m.UpdateAsync(target)).ReturnsAsync(IdentityResult.Success);

        var result = await controller.DeactivateUser(target.Id);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(target.IsDeactivated, Is.True);
    }

    [Test]
    public async Task DeactivateUser_RefusesToDeactivateAnAdminAccount()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var admin = TestHelpers.CreateUser(id: "admin-1", userName: "admin");
        var otherAdmin = TestHelpers.CreateUser(id: "admin-2", userName: "admin2");
        context.Users.AddRange(admin, otherAdmin);
        await context.SaveChangesAsync();

        var controller = CreateController(context, admin, out var userManagerMock);
        userManagerMock.Setup(m => m.FindByIdAsync(otherAdmin.Id)).ReturnsAsync(otherAdmin);
        userManagerMock.Setup(m => m.GetRolesAsync(otherAdmin)).ReturnsAsync(new List<string> { "Admin" });

        var result = await controller.DeactivateUser(otherAdmin.Id);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(otherAdmin.IsDeactivated, Is.False);
        userManagerMock.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Test]
    public async Task DeactivateUser_ReturnsNotFound_WhenUserDoesNotExist()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var admin = TestHelpers.CreateUser(id: "admin-1", userName: "admin");
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var controller = CreateController(context, admin, out var userManagerMock);
        userManagerMock.Setup(m => m.FindByIdAsync("missing-id")).ReturnsAsync((ApplicationUser?)null);

        var result = await controller.DeactivateUser("missing-id");

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task ActivateUser_ReactivatesADeactivatedUser()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var admin = TestHelpers.CreateUser(id: "admin-1", userName: "admin");
        var target = TestHelpers.CreateUser(id: "target-1", userName: "target");
        target.IsDeactivated = true;
        context.Users.AddRange(admin, target);
        await context.SaveChangesAsync();

        var controller = CreateController(context, admin, out var userManagerMock);
        userManagerMock.Setup(m => m.FindByIdAsync(target.Id)).ReturnsAsync(target);
        userManagerMock.Setup(m => m.UpdateAsync(target)).ReturnsAsync(IdentityResult.Success);

        var result = await controller.ActivateUser(target.Id);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(target.IsDeactivated, Is.False);
    }

    [Test]
    public async Task Users_FiltersBySearchTerm()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var admin = TestHelpers.CreateUser(id: "admin-1", userName: "admin");
        var alice = TestHelpers.CreateUser(id: "alice-1", userName: "alice", email: "alice@example.com");
        var bob = TestHelpers.CreateUser(id: "bob-1", userName: "bob", email: "bob@example.com");
        context.Users.AddRange(admin, alice, bob);
        await context.SaveChangesAsync();

        var controller = CreateController(context, admin, out _);

        var result = await controller.Users(search: "alice") as ViewResult;
        var model = result!.Model as List<BookNest.ViewModels.AdminUserViewModel>;

        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Any(u => u.UserName == "alice"), Is.True);
        Assert.That(model!.Any(u => u.UserName == "bob"), Is.False);
    }

    [Test]
    public async Task UserDetails_ReturnsNotFound_ForMissingUser()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var admin = TestHelpers.CreateUser(id: "admin-1", userName: "admin");
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var controller = CreateController(context, admin, out var userManagerMock);
        userManagerMock.Setup(m => m.FindByIdAsync("missing-id")).ReturnsAsync((ApplicationUser?)null);

        var result = await controller.UserDetails("missing-id");

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }
}
