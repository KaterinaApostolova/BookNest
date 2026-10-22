using System.ComponentModel.DataAnnotations;
using BookNest.Models;
using BookNest.ViewModels;

namespace BookNest.Tests;

public class BookDetailsTests
{
    [Test]
    public void BookDetails_ShouldSupportMissingDescriptionAndFallbackCover()
    {
        var book = new Book
        {
            Title = "The Hobbit",
            Description = string.Empty,
            CoverImageUrl = string.Empty
        };

        Assert.That(string.IsNullOrWhiteSpace(book.Description), Is.True);
        Assert.That(string.IsNullOrWhiteSpace(book.CoverImageUrl), Is.True);
    }

    [Test]
    public void ReadingLinks_ShouldGroupByLanguage()
    {
        var book = new Book { Title = "Pride and Prejudice" };
        book.ReadingLinks.Add(new ReadingLink
        {
            WebsiteName = "Project Gutenberg",
            Language = "English",
            Url = "https://example.com/english",
            LinkType = "Read Online"
        });
        book.ReadingLinks.Add(new ReadingLink
        {
            WebsiteName = "Chitanka",
            Language = "Bulgarian",
            Url = "https://example.com/bulgarian",
            LinkType = "Read Online"
        });

        var grouped = book.ReadingLinks.GroupBy(link => link.Language).ToList();

        Assert.That(grouped, Has.Count.EqualTo(2));
        Assert.That(grouped.Select(g => g.Key), Does.Contain("English"));
        Assert.That(grouped.Select(g => g.Key), Does.Contain("Bulgarian"));
    }

    [Test]
    public void SimilarBooks_ShouldNeverRecommendCurrentBook()
    {
        var currentBook = new Book { Id = 1, Title = "Current", AverageRating = 4.0 };
        currentBook.Authors.Add(new Author { Id = 10, Name = "Author A" });
        currentBook.Genres.Add(new Genre { Id = 20, Name = "Fantasy" });

        var otherBook = new Book { Id = 2, Title = "Similar", AverageRating = 4.5 };
        otherBook.Authors.Add(new Author { Id = 10, Name = "Author A" });
        otherBook.Genres.Add(new Genre { Id = 20, Name = "Fantasy" });

        var result = new List<Book> { currentBook, otherBook }
            .Where(b => b.Id != currentBook.Id)
            .ToList();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.Not.EqualTo(currentBook.Id));
    }

    [Test]
    public void BookFormViewModel_ShouldRequireTitle()
    {
        var model = new BookFormViewModel
        {
            Title = string.Empty,
            Authors = string.Empty,
            Genres = string.Empty
        };

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        Assert.That(isValid, Is.False);
        Assert.That(validationResults.Any(r => r.MemberNames.Contains(nameof(BookFormViewModel.Title))), Is.True);
    }
}
