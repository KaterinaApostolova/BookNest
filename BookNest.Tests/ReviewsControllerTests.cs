using BookNest.Controllers;
using BookNest.Models;
using BookNest.Services;
using BookNest.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BookNest.Tests;

public class ReviewsControllerTests
{
    private static ReviewsController CreateController(Data.BookNestDbContext context, ApplicationUser? currentUser, string? userId = null, params string[] roles)
    {
        var userManagerMock = TestHelpers.CreateUserManagerMock(currentUser);
        var translationService = new Mock<ITranslationService>();
        var controller = new ReviewsController(context, userManagerMock.Object, translationService.Object);

        if (currentUser is not null)
        {
            TestHelpers.AttachTempDataAndUser(controller, userId ?? currentUser.Id, roles);
        }
        else
        {
            TestHelpers.AttachTempData(controller);
        }

        return controller;
    }

    [Test]
    public async Task Create_Post_AddsReview_AndUpdatesBookAverageRating()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        var book = new Book { Title = "Dune" };
        context.Books.Add(book);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);
        var model = new ReviewViewModel { BookId = book.Id, Rating = 5, Content = "Amazing." };

        var result = await controller.Create(model);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var savedReview = await context.Reviews.FirstOrDefaultAsync(r => r.BookId == book.Id);
        Assert.That(savedReview, Is.Not.Null);
        Assert.That(savedReview!.UserId, Is.EqualTo(user.Id));

        var updatedBook = await context.Books.FirstAsync(b => b.Id == book.Id);
        Assert.That(updatedBook.AverageRating, Is.EqualTo(5));
    }

    [Test]
    public async Task Create_Post_PreventsDuplicateReview_FromSameUserOnSameBook()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        var book = new Book { Title = "Dune" };
        context.Books.Add(book);
        context.Users.Add(user);
        context.Reviews.Add(new Review { BookId = book.Id, UserId = user.Id, Rating = 4, Content = "First review" });
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);
        var model = new ReviewViewModel { BookId = book.Id, Rating = 5, Content = "Second attempt" };

        var result = await controller.Create(model);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(await context.Reviews.CountAsync(r => r.BookId == book.Id && r.UserId == user.Id), Is.EqualTo(1));
    }

    [Test]
    public async Task Edit_Post_Forbidden_WhenUserIsNotTheReviewAuthor()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var author = TestHelpers.CreateUser(id: "author-1", userName: "author");
        var otherUser = TestHelpers.CreateUser(id: "other-1", userName: "other");
        var book = new Book { Title = "Dune" };
        context.Books.Add(book);
        context.Users.AddRange(author, otherUser);
        await context.SaveChangesAsync();

        var review = new Review { BookId = book.Id, UserId = author.Id, Rating = 3, Content = "Original" };
        context.Reviews.Add(review);
        await context.SaveChangesAsync();

        var controller = CreateController(context, otherUser);
        var model = new ReviewViewModel { Id = review.Id, BookId = book.Id, Rating = 5, Content = "Hijacked edit" };

        var result = await controller.Edit(model);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
        var unchangedReview = await context.Reviews.FirstAsync(r => r.Id == review.Id);
        Assert.That(unchangedReview.Content, Is.EqualTo("Original"));
    }

    [Test]
    public async Task Edit_Post_UpdatesReview_WhenUserIsTheAuthor()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        var book = new Book { Title = "Dune" };
        context.Books.Add(book);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var review = new Review { BookId = book.Id, UserId = user.Id, Rating = 3, Content = "Original" };
        context.Reviews.Add(review);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);
        var model = new ReviewViewModel { Id = review.Id, BookId = book.Id, Rating = 5, Content = "Updated content" };

        var result = await controller.Edit(model);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var updatedReview = await context.Reviews.FirstAsync(r => r.Id == review.Id);
        Assert.That(updatedReview.Content, Is.EqualTo("Updated content"));
        Assert.That(updatedReview.Rating, Is.EqualTo(5));
    }

    [Test]
    public async Task DeleteConfirmed_RemovesReview_WhenUserIsTheAuthor()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var user = TestHelpers.CreateUser();
        var book = new Book { Title = "Dune" };
        context.Books.Add(book);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var review = new Review { BookId = book.Id, UserId = user.Id, Rating = 4, Content = "To delete" };
        context.Reviews.Add(review);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user);

        var result = await controller.DeleteConfirmed(review.Id);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(await context.Reviews.AnyAsync(r => r.Id == review.Id), Is.False);
    }

    [Test]
    public async Task DeleteConfirmed_Forbidden_WhenUserIsNotTheAuthor()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var author = TestHelpers.CreateUser(id: "author-1", userName: "author");
        var otherUser = TestHelpers.CreateUser(id: "other-1", userName: "other");
        var book = new Book { Title = "Dune" };
        context.Books.Add(book);
        context.Users.AddRange(author, otherUser);
        await context.SaveChangesAsync();

        var review = new Review { BookId = book.Id, UserId = author.Id, Rating = 4, Content = "Protected" };
        context.Reviews.Add(review);
        await context.SaveChangesAsync();

        var controller = CreateController(context, otherUser);

        var result = await controller.DeleteConfirmed(review.Id);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
        Assert.That(await context.Reviews.AnyAsync(r => r.Id == review.Id), Is.True);
    }
}
