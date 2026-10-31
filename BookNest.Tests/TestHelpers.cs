using System.Security.Claims;
using BookNest.Data;
using BookNest.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookNest.Tests;

public static class TestHelpers
{
    public static BookNestDbContext CreateInMemoryContext(string? name = null)
    {
        var options = new DbContextOptionsBuilder<BookNestDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
            .Options;

        return new BookNestDbContext(options);
    }

    /// <summary>
    /// Creates a mocked UserManager whose GetUserId/FindByIdAsync/FindByEmailAsync
    /// resolve against the given user, matching how controllers use it day to day.
    /// </summary>
    public static Mock<UserManager<ApplicationUser>> CreateUserManagerMock(ApplicationUser? currentUser = null)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var userManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        if (currentUser is not null)
        {
            userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(currentUser.Id);
            userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);
            userManager.Setup(m => m.FindByIdAsync(currentUser.Id)).ReturnsAsync(currentUser);
            userManager.Setup(m => m.FindByEmailAsync(currentUser.Email!)).ReturnsAsync(currentUser);
        }

        return userManager;
    }

    /// <summary>Builds a ClaimsPrincipal resembling what an authenticated request would carry.</summary>
    public static ClaimsPrincipal CreatePrincipal(string userId, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    public static ApplicationUser CreateUser(string id = "user-1", string userName = "reader1", string email = "reader1@example.com")
    {
        return new ApplicationUser
        {
            Id = id,
            UserName = userName,
            Email = email,
            FullName = "Test Reader",
            CreatedAt = DateTime.UtcNow,
            IsProfilePublic = true
        };
    }

    /// <summary>Creates a mocked RoleManager (needed by ModerationController).</summary>
    public static Mock<RoleManager<IdentityRole>> CreateRoleManagerMock()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        return new Mock<RoleManager<IdentityRole>>(store.Object, null!, null!, null!, null!);
    }

    /// <summary>Creates a mocked SignInManager backed by the given UserManager mock.</summary>
    public static Mock<SignInManager<ApplicationUser>> CreateSignInManagerMock(Mock<UserManager<ApplicationUser>> userManagerMock)
    {
        var contextAccessor = new Mock<IHttpContextAccessor> { DefaultValue = DefaultValue.Mock };
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();

        return new Mock<SignInManager<ApplicationUser>>(
            userManagerMock.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            null!, null!, null!, null!);
    }

    /// <summary>
    /// Controllers set TempData["..."] in several actions (e.g. success/error banners).
    /// Outside a real HTTP request, TempData has no backing store — this gives it one
    /// so those assignments don't throw when a controller action is called directly.
    /// </summary>
    public static void AttachTempData(Controller controller)
    {
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
    }

    /// <summary>Attaches TempData and sets HttpContext.User to a fake authenticated principal.</summary>
    public static void AttachTempDataAndUser(Controller controller, string userId, params string[] roles)
    {
        var httpContext = new DefaultHttpContext
        {
            User = CreatePrincipal(userId, roles)
        };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
    }

    /// <summary>
    /// Attaches a minimal but functional IUrlHelper so extension methods like
    /// Url.IsLocalUrl(...) work correctly inside the action under test.
    /// </summary>
    public static void AttachUrlHelper(Controller controller)
    {
        var httpContext = controller.ControllerContext.HttpContext ?? new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");
        controller.ControllerContext.HttpContext = httpContext;

        var actionContext = new ActionContext(
            httpContext,
            new Microsoft.AspNetCore.Routing.RouteData(),
            new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

        controller.Url = new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(actionContext);
    }
}
