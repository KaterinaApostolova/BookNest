using BookNest.Controllers;
using BookNest.Models;
using BookNest.Services;
using BookNest.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BookNest.Tests;

public class ProfileControllerTests
{
    private static ProfileController CreateController(
        Data.BookNestDbContext context,
        ApplicationUser? currentUser,
        out Mock<UserManager<ApplicationUser>> userManagerMock)
    {
        userManagerMock = TestHelpers.CreateUserManagerMock(currentUser);
        userManagerMock.Setup(m => m.Users).Returns(context.Users);
        var translationService = new Mock<ITranslationService>();
        var controller = new ProfileController(context, userManagerMock.Object, translationService.Object);

        if (currentUser is not null)
        {
            TestHelpers.AttachTempDataAndUser(controller, currentUser.Id);
        }
        else
        {
            TestHelpers.AttachTempData(controller);
        }

        return controller;
    }

    [Test]
    public async Task Edit_Post_RedirectsToLogin_WhenUserIsMissing()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var controller = CreateController(context, currentUser: null, out _);

        var result = await controller.Edit(new EditProfileViewModel());

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(((RedirectToActionResult)result).ActionName, Is.EqualTo("Login"));
    }

    [Test]
    public async Task Edit_Post_ShowsError_WhenNewUsernameIsAlreadyTaken()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser(id: "user-1", userName: "reader1");
        var otherUser = TestHelpers.CreateUser(id: "user-2", userName: "takenname");
        context.Users.AddRange(user, otherUser);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user, out var userManagerMock);
        userManagerMock.Setup(m => m.FindByNameAsync("takenname")).ReturnsAsync(otherUser);

        var model = new EditProfileViewModel
        {
            Username = "takenname",
            Email = user.Email!,
            FullName = user.FullName
        };

        var result = await controller.Edit(model);

        Assert.That(result, Is.InstanceOf<ViewResult>());
        Assert.That(controller.ModelState.IsValid, Is.False);
    }

    [Test]
    public async Task RemoveFavouriteQuote_ClearsQuote()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        user.FavouriteQuote = "Some quote";
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user, out var userManagerMock);
        userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await controller.RemoveFavouriteQuote();

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(user.FavouriteQuote, Is.Null);
    }

    [Test]
    public async Task Public_ReturnsNotFound_WhenUsernameDoesNotExist()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var controller = CreateController(context, currentUser: null, out _);

        var result = await controller.Public("ghost-user");

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Public_ReturnsAnonymousProfileView_WhenProfileIsPrivate()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var privateUser = TestHelpers.CreateUser(id: "user-1", userName: "privateuser");
        privateUser.IsProfilePublic = false;
        context.Users.Add(privateUser);
        await context.SaveChangesAsync();

        var controller = CreateController(context, currentUser: null, out _);

        var result = await controller.Public("privateuser") as ViewResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ViewName, Is.EqualTo("AnonymousProfile"));
    }

    [Test]
    public async Task Public_ReturnsProfile_WhenProfileIsPublic()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var publicUser = TestHelpers.CreateUser(id: "user-1", userName: "publicuser");
        publicUser.IsProfilePublic = true;
        context.Users.Add(publicUser);
        await context.SaveChangesAsync();

        var controller = CreateController(context, currentUser: null, out _);

        var result = await controller.Public("publicuser") as ViewResult;

        Assert.That(result, Is.Not.Null);
        var model = result!.Model as PublicProfileViewModel;
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.UserName, Is.EqualTo("publicuser"));
    }
}
