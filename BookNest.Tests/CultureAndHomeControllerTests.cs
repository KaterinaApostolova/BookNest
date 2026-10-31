using BookNest.Controllers;
using BookNest.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookNest.Tests;

public class CultureControllerTests
{
    private static CultureController CreateController()
    {
        var controller = new CultureController();
        TestHelpers.AttachTempData(controller);
        TestHelpers.AttachUrlHelper(controller);
        return controller;
    }

    [Test]
    public void Set_RedirectsToLocalUrl_WhenReturnUrlIsLocal()
    {
        var controller = CreateController();

        var result = controller.Set("bg", "/Books");

        Assert.That(result, Is.InstanceOf<LocalRedirectResult>());
        Assert.That(((LocalRedirectResult)result).Url, Is.EqualTo("/Books"));
    }

    [Test]
    public void Set_SetsCultureCookie()
    {
        var controller = CreateController();

        controller.Set("bg", "/Books");

        var setCookieHeader = controller.Response.Headers["Set-Cookie"].ToString();
        Assert.That(setCookieHeader, Does.Contain("bg"));
    }

    [Test]
    public void Set_FallsBackToRootPath_WhenReturnUrlIsNotLocal()
    {
        var controller = CreateController();

        var result = controller.Set("en", "https://evil-site.example.com");

        Assert.That(result, Is.InstanceOf<LocalRedirectResult>());
        Assert.That(((LocalRedirectResult)result).Url, Is.EqualTo("/"));
    }

    [Test]
    public void Set_DefaultsToEnglish_WhenCultureIsNotRecognized()
    {
        var controller = CreateController();

        controller.Set("fr", "/Books");

        var setCookieHeader = controller.Response.Headers["Set-Cookie"].ToString();
        Assert.That(setCookieHeader, Does.Contain("c%3Den"));
    }
}

public class HomeControllerTests
{
    [Test]
    public async Task Index_ReturnsFeaturedBooks()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        for (var i = 1; i <= 5; i++)
        {
            context.Books.Add(new Models.Book { Title = $"Book {i}" });
        }
        await context.SaveChangesAsync();

        var homeViewService = new Services.HomeViewService(context);
        var controller = new HomeController(homeViewService,null);

        var result = await controller.Index() as ViewResult;
        var model = result!.Model as BookNest.ViewModels.HomeViewModel;

        Assert.That(model, Is.Not.Null);
        Assert.That(model!.FeaturedBooks, Is.Not.Empty);
    }

    [Test]
    public void Privacy_ReturnsView()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var homeViewService = new Services.HomeViewService(context);
        var controller = new HomeController(homeViewService,null);

        var result = controller.Privacy();

        Assert.That(result, Is.InstanceOf<ViewResult>());
    }
}
