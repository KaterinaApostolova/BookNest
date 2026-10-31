using BookNest.Data;
using BookNest.Models;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Tests;

public class DbContextModelTests
{
    private static BookNestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BookNestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BookNestDbContext(options);
    }

    [Test]
    public async Task Book_CanHaveMultipleGenresAndAuthors()
    {
        using var context = CreateContext();

        var book = new Book { Title = "Dune" };
        var author = new Author { Name = "Frank Herbert" };
        var genreOne = new Genre { Name = "Science Fiction" };
        var genreTwo = new Genre { Name = "Adventure" };

        book.Authors.Add(author);
        book.Genres.Add(genreOne);
        book.Genres.Add(genreTwo);

        context.Books.Add(book);
        await context.SaveChangesAsync();

        var savedBook = await context.Books
            .Include(b => b.Authors)
            .Include(b => b.Genres)
            .FirstAsync(b => b.Title == "Dune");

        Assert.That(savedBook.Authors, Has.Count.EqualTo(1));
        Assert.That(savedBook.Genres, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Review_PersistsIsAnonymousFlag()
    {
        using var context = CreateContext();

        var book = new Book { Title = "1984" };
        context.Books.Add(book);
        await context.SaveChangesAsync();

        context.Reviews.Add(new Review
        {
            BookId = book.Id,
            Rating = 5,
            Content = "A must-read.",
            IsAnonymous = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var savedReview = await context.Reviews.FirstAsync(r => r.BookId == book.Id);

        Assert.That(savedReview.IsAnonymous, Is.True);
    }

    [Test]
    public async Task Forum_CanBeLinkedToASpecificBook()
    {
        using var context = CreateContext();

        var book = new Book { Title = "Frankenstein" };
        context.Books.Add(book);
        await context.SaveChangesAsync();

        context.Forums.Add(new Forum
        {
            Name = "Frankenstein Discussion",
            NormalizedName = "FRANKENSTEIN DISCUSSION",
            BookId = book.Id
        });
        await context.SaveChangesAsync();

        var forum = await context.Forums.Include(f => f.Book).FirstAsync(f => f.BookId == book.Id);

        Assert.That(forum.Book, Is.Not.Null);
        Assert.That(forum.Book!.Title, Is.EqualTo("Frankenstein"));
    }

    [Test]
    public async Task ForumTopic_TracksPinnedLockedAndViewCountIndependently()
    {
        using var context = CreateContext();

        var forum = new Forum { Name = "General", NormalizedName = "GENERAL" };
        context.Forums.Add(forum);
        await context.SaveChangesAsync();

        var topic = new ForumTopic
        {
            ForumId = forum.Id,
            Title = "Welcome thread",
            NormalizedTitle = "WELCOME THREAD",
            Content = "Say hello!",
            IsPinned = true,
            IsLocked = false,
            ViewCount = 0
        };
        context.ForumTopics.Add(topic);
        await context.SaveChangesAsync();

        topic.ViewCount += 1;
        await context.SaveChangesAsync();

        var savedTopic = await context.ForumTopics.FirstAsync(t => t.Id == topic.Id);

        Assert.That(savedTopic.IsPinned, Is.True);
        Assert.That(savedTopic.IsLocked, Is.False);
        Assert.That(savedTopic.ViewCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ForumReaction_CanBeAttachedToEitherATopicOrAPost_ButRepresentsOneVote()
    {
        using var context = CreateContext();

        var forum = new Forum { Name = "General", NormalizedName = "GENERAL" };
        context.Forums.Add(forum);
        await context.SaveChangesAsync();

        var topic = new ForumTopic
        {
            ForumId = forum.Id,
            Title = "Topic",
            NormalizedTitle = "TOPIC",
            Content = "Body"
        };
        context.ForumTopics.Add(topic);
        await context.SaveChangesAsync();

        context.ForumReactions.Add(new ForumReaction
        {
            UserId = "user-1",
            ForumTopicId = topic.Id,
            IsLike = true
        });
        await context.SaveChangesAsync();

        var reaction = await context.ForumReactions.FirstAsync(r => r.ForumTopicId == topic.Id);

        Assert.That(reaction.IsLike, Is.True);
        Assert.That(reaction.ForumPostId, Is.Null);
    }

    [Test]
    public async Task DeletingATopic_AlsoRemovesItsPosts_ViaCascade()
    {
        using var context = CreateContext();

        var forum = new Forum { Name = "General", NormalizedName = "GENERAL" };
        context.Forums.Add(forum);
        await context.SaveChangesAsync();

        var topic = new ForumTopic
        {
            ForumId = forum.Id,
            Title = "Topic",
            NormalizedTitle = "TOPIC",
            Content = "Body"
        };
        context.ForumTopics.Add(topic);
        await context.SaveChangesAsync();

        context.ForumPosts.Add(new ForumPost
        {
            ForumTopicId = topic.Id,
            UserId = "user-1",
            Content = "A reply"
        });
        await context.SaveChangesAsync();

        context.ForumTopics.Remove(topic);
        await context.SaveChangesAsync();

        var remainingPosts = await context.ForumPosts.Where(p => p.ForumTopicId == topic.Id).ToListAsync();

        Assert.That(remainingPosts, Is.Empty);
    }

    [Test]
    public async Task ReadingList_TracksStatusPerUserPerBook()
    {
        using var context = CreateContext();

        var status = new ReadingStatus { Name = "Completed" };
        var book = new Book { Title = "Moby Dick" };
        context.ReadingStatuses.Add(status);
        context.Books.Add(book);
        await context.SaveChangesAsync();

        context.ReadingLists.Add(new ReadingList
        {
            Name = "My Books",
            UserId = "user-1",
            BookId = book.Id,
            ReadingStatusId = status.Id
        });
        await context.SaveChangesAsync();

        var entry = await context.ReadingLists
            .Include(r => r.ReadingStatus)
            .FirstAsync(r => r.UserId == "user-1" && r.BookId == book.Id);

        Assert.That(entry.ReadingStatus!.Name, Is.EqualTo("Completed"));
    }
}
