using BookNest.Controllers;
using BookNest.Models;
using BookNest.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Tests;

public class ReadingLinksControllerTests
{
    private static ReadingLinksController CreateController(Data.BookNestDbContext context)
    {
        var controller = new ReadingLinksController(context);
        TestHelpers.AttachTempData(controller);
        return controller;
    }

    [Test]
    public async Task AddReadingLink_Post_AddsLink_ForExistingBook()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var book = new Book { Title = "Dune" };
        context.Books.Add(book);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var model = new ReadingLinkViewModel
        {
            BookId = book.Id,
            WebsiteName = "Chitanka",
            Language = "Bulgarian",
            Url = "https://chitanka.info/dune",
            LinkType = "Read Online"
        };

        var result = await controller.AddReadingLink(model);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var savedLink = await context.ReadingLinks.FirstOrDefaultAsync(l => l.BookId == book.Id);
        Assert.That(savedLink, Is.Not.Null);
        Assert.That(savedLink!.WebsiteName, Is.EqualTo("Chitanka"));
    }

    [Test]
    public async Task AddReadingLink_Post_ReturnsNotFound_WhenBookDoesNotExist()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var controller = CreateController(context);
        var model = new ReadingLinkViewModel
        {
            BookId = 999,
            WebsiteName = "Chitanka",
            Language = "Bulgarian",
            Url = "https://chitanka.info/x",
            LinkType = "Read Online"
        };

        var result = await controller.AddReadingLink(model);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task EditReadingLink_Post_UpdatesExistingLink()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var book = new Book { Title = "Dune" };
        context.Books.Add(book);
        await context.SaveChangesAsync();

        var link = new ReadingLink
        {
            BookId = book.Id,
            WebsiteName = "Old Site",
            Language = "English",
            Url = "https://old.example.com",
            LinkType = "Buy"
        };
        context.ReadingLinks.Add(link);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var model = new ReadingLinkViewModel
        {
            Id = link.Id,
            BookId = book.Id,
            WebsiteName = "New Site",
            Language = "English",
            Url = "https://new.example.com",
            LinkType = "Buy"
        };

        var result = await controller.EditReadingLink(link.Id, model);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var updated = await context.ReadingLinks.FirstAsync(l => l.Id == link.Id);
        Assert.That(updated.WebsiteName, Is.EqualTo("New Site"));
        Assert.That(updated.Url, Is.EqualTo("https://new.example.com"));
    }

    [Test]
    public async Task DeleteReadingLink_RemovesLink()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var book = new Book { Title = "Dune" };
        context.Books.Add(book);
        await context.SaveChangesAsync();

        var link = new ReadingLink
        {
            BookId = book.Id,
            WebsiteName = "Site",
            Language = "English",
            Url = "https://example.com",
            LinkType = "Buy"
        };
        context.ReadingLinks.Add(link);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.DeleteReadingLink(link.Id);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(await context.ReadingLinks.AnyAsync(l => l.Id == link.Id), Is.False);
    }

    [Test]
    public async Task DeleteReadingLink_ReturnsNotFound_WhenLinkDoesNotExist()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var controller = CreateController(context);

        var result = await controller.DeleteReadingLink(999);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }
}
