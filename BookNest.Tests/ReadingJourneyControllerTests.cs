using BookNest.Controllers;
using BookNest.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Tests;

public class ReadingJourneyControllerTests
{
    private static ReadingJourneyController CreateController(Data.BookNestDbContext context, ApplicationUser user)
    {
        var userManagerMock = TestHelpers.CreateUserManagerMock(user);
        var controller = new ReadingJourneyController(context, userManagerMock.Object);
        TestHelpers.AttachTempDataAndUser(controller, user.Id);
        return controller;
    }

    private static async Task SeedStatusesAsync(Data.BookNestDbContext context)
    {
        context.ReadingStatuses.AddRange(
            new ReadingStatus { Name = "Want to Read" },
            new ReadingStatus { Name = "Reading" },
            new ReadingStatus { Name = "Completed" });
        await context.SaveChangesAsync();
    }

    [Test]
    public async Task AddToReadingJourney_CreatesEntry_WithWantToReadStatus()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        var book = new Book { Title = "Dune" };
        context.Users.Add(user);
        context.Books.Add(book);
        await SeedStatusesAsync(context);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);

        var result = await controller.AddToReadingJourney(book.Id);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var entry = await context.ReadingLists
            .Include(r => r.ReadingStatus)
            .FirstOrDefaultAsync(r => r.BookId == book.Id && r.UserId == user.Id);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.ReadingStatus!.Name, Is.EqualTo("Want to Read"));
    }

    [Test]
    public async Task AddToReadingJourney_DoesNotDuplicate_WhenBookAlreadyInJourney()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        var book = new Book { Title = "Dune" };
        context.Users.Add(user);
        context.Books.Add(book);
        await SeedStatusesAsync(context);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);

        await controller.AddToReadingJourney(book.Id);
        await controller.AddToReadingJourney(book.Id);

        var count = await context.ReadingLists.CountAsync(r => r.BookId == book.Id && r.UserId == user.Id);
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task UpdateReadingJourney_ChangesStatus_AndSetsCompletionTempData_WhenMarkedCompleted()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        var book = new Book { Title = "Dune" };
        context.Users.Add(user);
        context.Books.Add(book);
        await SeedStatusesAsync(context);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);
        await controller.AddToReadingJourney(book.Id);

        var result = await controller.UpdateReadingJourney(book.Id, "Completed");

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var entry = await context.ReadingLists
            .Include(r => r.ReadingStatus)
            .FirstAsync(r => r.BookId == book.Id && r.UserId == user.Id);
        Assert.That(entry.ReadingStatus!.Name, Is.EqualTo("Completed"));
        Assert.That(controller.TempData["CompletedBookId"], Is.EqualTo(book.Id));
    }

    [Test]
    public async Task UpdateReadingJourney_ReturnsBadRequest_ForUnknownStatus()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        var book = new Book { Title = "Dune" };
        context.Users.Add(user);
        context.Books.Add(book);
        await SeedStatusesAsync(context);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);
        await controller.AddToReadingJourney(book.Id);

        var result = await controller.UpdateReadingJourney(book.Id, "NotARealStatus");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task RemoveFromReadingJourney_RemovesOwnEntry()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        var book = new Book { Title = "Dune" };
        context.Users.Add(user);
        context.Books.Add(book);
        await SeedStatusesAsync(context);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);
        await controller.AddToReadingJourney(book.Id);
        var entry = await context.ReadingLists.FirstAsync(r => r.BookId == book.Id && r.UserId == user.Id);

        var result = await controller.RemoveFromReadingJourney(entry.Id);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(await context.ReadingLists.AnyAsync(r => r.Id == entry.Id), Is.False);
    }

    [Test]
    public async Task RemoveFromReadingJourney_ReturnsNotFound_ForAnotherUsersEntry()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var owner = TestHelpers.CreateUser(id: "owner-1", userName: "owner");
        var otherUser = TestHelpers.CreateUser(id: "other-1", userName: "other");
        var book = new Book { Title = "Dune" };
        context.Users.AddRange(owner, otherUser);
        context.Books.Add(book);
        await SeedStatusesAsync(context);
        await context.SaveChangesAsync();

        var ownerController = CreateController(context, owner);
        await ownerController.AddToReadingJourney(book.Id);
        var entry = await context.ReadingLists.FirstAsync(r => r.BookId == book.Id && r.UserId == owner.Id);

        var otherController = CreateController(context, otherUser);
        var result = await otherController.RemoveFromReadingJourney(entry.Id);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
        Assert.That(await context.ReadingLists.AnyAsync(r => r.Id == entry.Id), Is.True);
    }
}
