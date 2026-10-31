using BookNest.Controllers;
using BookNest.Models;
using BookNest.Services;
using BookNest.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BookNest.Tests;

public class BooksControllerTests
{
    private static BooksController CreateController(Data.BookNestDbContext context)
    {
        var translationService = new Mock<ITranslationService>();
        var controller = new BooksController(context, translationService.Object);
        TestHelpers.AttachTempData(controller);
        return controller;
    }

    [Test]
    public async Task Create_Post_AddsBookWithAuthorsAndGenres_AndCreatesItsForum()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var controller = CreateController(context);

        var model = new BookFormViewModel
        {
            Title = "The Hobbit",
            Description = "A hobbit's journey.",
            Authors = "J.R.R. Tolkien",
            Genres = "Fantasy, Classic"
        };

        var result = await controller.Create(model);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());

        var savedBook = await context.Books
            .Include(b => b.Authors)
            .Include(b => b.Genres)
            .FirstOrDefaultAsync(b => b.Title == "The Hobbit");

        Assert.That(savedBook, Is.Not.Null);
        Assert.That(savedBook!.Authors.Select(a => a.Name), Does.Contain("J.R.R. Tolkien"));
        Assert.That(savedBook.Genres, Has.Count.EqualTo(2));

        // Creating a book should also create its discussion forum automatically.
        var forum = await context.Forums.FirstOrDefaultAsync(f => f.BookId == savedBook.Id);
        Assert.That(forum, Is.Not.Null);
    }

    [Test]
    public async Task Create_Post_ReturnsView_WhenAuthorsFieldIsEmpty()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var controller = CreateController(context);

        var model = new BookFormViewModel
        {
            Title = "Untitled",
            Authors = string.Empty,
            Genres = "Fantasy"
        };

        var result = await controller.Create(model);

        Assert.That(result, Is.InstanceOf<ViewResult>());
        Assert.That(controller.ModelState.IsValid, Is.False);
        Assert.That(await context.Books.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task Details_ReturnsBook_WhenItExists()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var book = new Book { Title = "Dune" };
        context.Books.Add(book);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.Details(book.Id) as ViewResult;

        Assert.That(result, Is.Not.Null);
        Assert.That((result!.Model as Book)?.Title, Is.EqualTo("Dune"));
    }

    [Test]
    public async Task Details_ReturnsNotFound_WhenBookDoesNotExist()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var controller = CreateController(context);

        var result = await controller.Details(999);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Edit_Post_UpdatesBookTitle()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var author = new Author { Name = "Author A" };
        var genre = new Genre { Name = "Fantasy" };
        var book = new Book { Title = "Old Title" };
        book.Authors.Add(author);
        book.Genres.Add(genre);
        context.Books.Add(book);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var model = new BookFormViewModel
        {
            Id = book.Id,
            Title = "New Title",
            Authors = "Author A",
            Genres = "Fantasy"
        };

        var result = await controller.Edit(book.Id, model);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var updatedBook = await context.Books.FirstAsync(b => b.Id == book.Id);
        Assert.That(updatedBook.Title, Is.EqualTo("New Title"));
    }

    [Test]
    public async Task DeleteConfirmed_RemovesBook()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        var book = new Book { Title = "To Delete" };
        context.Books.Add(book);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.DeleteConfirmed(book.Id);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(await context.Books.AnyAsync(b => b.Id == book.Id), Is.False);
    }

    [Test]
    public async Task Index_FiltersBySearchTerm()
    {
        using var context = TestHelpers.CreateInMemoryContext();
        context.Books.Add(new Book { Title = "Dune" });
        context.Books.Add(new Book { Title = "1984" });
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.Index(searchTerm: "Dune", genreId: null, sortOrder: null) as ViewResult;
        var model = result!.Model as IEnumerable<Book>;

        Assert.That(model!.Count(), Is.EqualTo(1));
        Assert.That(model!.First().Title, Is.EqualTo("Dune"));
    }
}
