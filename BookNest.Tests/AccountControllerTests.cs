using BookNest.Controllers;
using BookNest.Models;
using BookNest.Services;
using BookNest.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BookNest.Tests;

public class AccountControllerTests
{
    private static (AccountController Controller, Mock<UserManager<ApplicationUser>> UserManager, Mock<SignInManager<ApplicationUser>> SignInManager)
        CreateController()
    {
        var userManagerMock = TestHelpers.CreateUserManagerMock();
        var signInManagerMock = TestHelpers.CreateSignInManagerMock(userManagerMock);
        var quoteService = new QuoteService();
        var emailSenderMock = new Mock<IEmailSender>();

        var controller = new AccountController(userManagerMock.Object, signInManagerMock.Object, quoteService, emailSenderMock.Object);
        TestHelpers.AttachTempData(controller);
        TestHelpers.AttachUrlHelper(controller);

        return (controller, userManagerMock, signInManagerMock);
    }

    [Test]
    public async Task Register_Post_CreatesUser_AssignsUserRole_AndSignsIn_WhenSuccessful()
    {
        var (controller, userManagerMock, signInManagerMock) = CreateController();

        userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "StrongPass1!"))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        var model = new RegisterViewModel
        {
            FullName = "Jane Reader",
            Username = "janereader",
            Email = "jane@example.com",
            Password = "StrongPass1!",
            ConfirmPassword = "StrongPass1!"
        };

        var result = await controller.Register(model);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"), Times.Once);
        signInManagerMock.Verify(m => m.SignInAsync(It.IsAny<ApplicationUser>(), false, null), Times.Once);
    }

    [Test]
    public async Task Register_Post_ReturnsView_WhenPasswordIsRejectedByIdentity()
    {
        var (controller, userManagerMock, _) = CreateController();

        userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak." }));

        var model = new RegisterViewModel
        {
            FullName = "Jane Reader",
            Username = "janereader",
            Email = "jane@example.com",
            Password = "weak",
            ConfirmPassword = "weak"
        };

        var result = await controller.Register(model);

        Assert.That(result, Is.InstanceOf<ViewResult>());
        Assert.That(controller.ModelState.IsValid, Is.False);
    }

    [Test]
    public async Task Login_Post_ShowsError_WhenUserDoesNotExist()
    {
        var (controller, userManagerMock, _) = CreateController();
        userManagerMock.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
        userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var model = new LoginViewModel { UsernameOrEmail = "ghost", Password = "whatever" };

        var result = await controller.Login(model);

        Assert.That(result, Is.InstanceOf<ViewResult>());
        Assert.That(controller.ModelState.IsValid, Is.False);
    }

    [Test]
    public async Task Login_Post_ShowsError_WhenAccountIsDeactivated()
    {
        var (controller, userManagerMock, _) = CreateController();
        var user = TestHelpers.CreateUser();
        user.IsDeactivated = true;
        userManagerMock.Setup(m => m.FindByNameAsync(user.UserName!)).ReturnsAsync(user);

        var model = new LoginViewModel { UsernameOrEmail = user.UserName!, Password = "whatever" };

        var result = await controller.Login(model);

        Assert.That(result, Is.InstanceOf<ViewResult>());
        Assert.That(controller.ModelState.IsValid, Is.False);
    }

    [Test]
    public async Task Login_Post_RedirectsToWelcome_WhenCredentialsAreValid()
    {
        var (controller, userManagerMock, signInManagerMock) = CreateController();
        var user = TestHelpers.CreateUser();
        userManagerMock.Setup(m => m.FindByNameAsync(user.UserName!)).ReturnsAsync(user);
        signInManagerMock
            .Setup(m => m.PasswordSignInAsync(user, "CorrectPass1!", false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var model = new LoginViewModel { UsernameOrEmail = user.UserName!, Password = "CorrectPass1!" };

        var result = await controller.Login(model);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(((RedirectToActionResult)result).ActionName, Is.EqualTo("Welcome"));
    }

    [Test]
    public async Task ForgotPassword_Post_AlwaysShowsSameMessage_RegardlessOfWhetherAccountExists()
    {
        var (controller, userManagerMock, _) = CreateController();
        userManagerMock.Setup(m => m.FindByEmailAsync("unknown@example.com")).ReturnsAsync((ApplicationUser?)null);

        var model = new ForgotPasswordViewModel { Email = "unknown@example.com" };

        var result = await controller.ForgotPassword(model);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(controller.TempData["InfoMessage"], Is.Not.Null);
    }

    [Test]
    public async Task ResetPassword_Post_ShowsErrors_WhenTokenIsInvalid()
    {
        var (controller, userManagerMock, _) = CreateController();
        var user = TestHelpers.CreateUser();
        userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManagerMock
            .Setup(m => m.ResetPasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));

        var model = new ResetPasswordViewModel
        {
            UserId = user.Id,
            Token = "bad-token",
            Password = "NewPass1!",
            ConfirmPassword = "NewPass1!"
        };

        var result = await controller.ResetPassword(model);

        Assert.That(result, Is.InstanceOf<ViewResult>());
        Assert.That(controller.ModelState.IsValid, Is.False);
    }
}
