using BookNest.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<BookNestDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await context.Database.MigrateAsync();

        if (!await context.Genres.AnyAsync())
        {
            context.Genres.AddRange(
                new Genre { Name = "Fantasy" },
                new Genre { Name = "Science Fiction" },
                new Genre { Name = "Classic" });
        }

        if (!await context.ReadingStatuses.AnyAsync())
        {
            context.ReadingStatuses.AddRange(
                new ReadingStatus { Name = "Want to Read" },
                new ReadingStatus { Name = "Reading" },
                new ReadingStatus { Name = "Completed" });
        }

        var defaultForums = new[]
        {
            new { Name = "General Talk", Description = "Share your reading thoughts and connect with other readers." },
            new { Name = "Book Recommendations", Description = "Discover your next read and recommend books to the community." },
            new { Name = "Authors and Writing", Description = "Talk about authors, writing, and the stories behind the books." }
        };

        foreach (var defaultForum in defaultForums)
        {
            if (!await context.Forums.AnyAsync(f => f.Name == defaultForum.Name))
            {
                context.Forums.Add(new Forum
                {
                    Name = defaultForum.Name,
                    NormalizedName = defaultForum.Name.Trim().ToUpperInvariant(),
                    Description = defaultForum.Description
                });
            }
        }

        if (!await context.Authors.AnyAsync())
        {
            context.Authors.Add(new Author { Name = "Jane Austen", Biography = "A beloved classic author." });
            context.Authors.Add(new Author { Name = "Octavia Butler", Biography = "A visionary science fiction writer." });
            context.Authors.Add(new Author { Name = "Haruki Murakami", Biography = "A contemporary literary storyteller." });
        }

        await context.SaveChangesAsync();

        if (!await context.Books.AnyAsync())
        {
            var authors = await context.Authors.ToListAsync();
            var genres = await context.Genres.ToListAsync();

            var author1 = authors.FirstOrDefault(a => a.Name == "Jane Austen");
            var author2 = authors.FirstOrDefault(a => a.Name == "Octavia Butler");
            var author3 = authors.FirstOrDefault(a => a.Name == "Haruki Murakami");
            var genreFantasy = genres.FirstOrDefault(g => g.Name == "Fantasy");
            var genreSciFi = genres.FirstOrDefault(g => g.Name == "Science Fiction");
            var genreClassic = genres.FirstOrDefault(g => g.Name == "Classic");

            if (author1 is not null && author2 is not null && author3 is not null && genreFantasy is not null && genreSciFi is not null && genreClassic is not null)
            {
                context.Books.AddRange(
                    new Book
                    {
                        Title = "Pride and Prejudice",
                        Description = "A timeless story about first impressions, manners, and personal growth.",
                        PublicationYear = 1813,
                        CoverImageUrl = "/images/books/cover-placeholder.svg",
                        AverageRating = 4.6,
                        Authors = new List<Author> { author1 },
                        Genres = new List<Genre> { genreClassic }
                    },
                    new Book
                    {
                        Title = "Kindred",
                        Description = "A gripping tale that blends history and imagination with sharp emotional power.",
                        PublicationYear = 1979,
                        CoverImageUrl = "/images/books/cover-placeholder.svg",
                        AverageRating = 4.4,
                        Authors = new List<Author> { author2 },
                        Genres = new List<Genre> { genreSciFi }
                    },
                    new Book
                    {
                        Title = "Kafka on the Shore",
                        Description = "A surreal and moving novel that invites readers into a dreamlike world.",
                        PublicationYear = 2002,
                        CoverImageUrl = "/images/books/cover-placeholder.svg",
                        AverageRating = 4.2,
                        Authors = new List<Author> { author3 },
                        Genres = new List<Genre> { genreFantasy }
                    });
            }

            await context.SaveChangesAsync();
        }

        if (!await context.ReadingLinks.AnyAsync())
        {
            var books = await context.Books.ToListAsync();
            var pride = books.FirstOrDefault(b => b.Title == "Pride and Prejudice");
            var kindred = books.FirstOrDefault(b => b.Title == "Kindred");
            var kafka = books.FirstOrDefault(b => b.Title == "Kafka on the Shore");

            if (pride is not null)
            {
                context.ReadingLinks.AddRange(
                    new ReadingLink { BookId = pride.Id, WebsiteName = "Project Gutenberg", Language = "English", Url = "https://www.gutenberg.org/ebooks/1342", LinkType = "Read Online" },
                    new ReadingLink { BookId = pride.Id, WebsiteName = "Amazon", Language = "English", Url = "https://www.amazon.com/", LinkType = "Buy" });
            }

            if (kindred is not null)
            {
                context.ReadingLinks.Add(new ReadingLink { BookId = kindred.Id, WebsiteName = "Open Library", Language = "English", Url = "https://openlibrary.org/", LinkType = "Borrow" });
            }

            if (kafka is not null)
            {
                context.ReadingLinks.Add(new ReadingLink { BookId = kafka.Id, WebsiteName = "Bookshop", Language = "English", Url = "https://www.bookshop.org/", LinkType = "Buy" });
            }
        }

        await context.SaveChangesAsync();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(new IdentityRole("User"));
        }

        var adminEmail = "admin@booknest.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "booknestadmin",
                Email = adminEmail,
                FullName = "BookNest Admin"
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                await userManager.AddToRoleAsync(adminUser, "User");
            }
        }
        else
        {
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            if (!await userManager.IsInRoleAsync(adminUser, "User"))
            {
                await userManager.AddToRoleAsync(adminUser, "User");
            }
        }

        var starterTopics = new[]
        {
            new
            {
                ForumName = "General Talk",
                Title = "What are you reading this week?",
                Content = "Share what is currently on your reading list and what you are enjoying about it."
            },
            new
            {
                ForumName = "General Talk",
                Title = "How do you make time for reading?",
                Content = "What reading habits or routines help you find time for books?"
            },
            new
            {
                ForumName = "Book Recommendations",
                Title = "Recommend a book that stayed with you",
                Content = "Tell the community about a book you still think about and why you recommend it."
            },
            new
            {
                ForumName = "Book Recommendations",
                Title = "Finding a new favourite fantasy",
                Content = "Which fantasy books would you recommend to someone looking for a new favourite?"
            },
            new
            {
                ForumName = "Authors and Writing",
                Title = "Authors whose work you want to explore",
                Content = "Which author would you like to read more widely, and where should a new reader begin?"
            },
            new
            {
                ForumName = "Authors and Writing",
                Title = "What makes an ending memorable?",
                Content = "Discuss the writing choices that make a book's ending satisfying or unforgettable."
            }
        };

        foreach (var starterTopic in starterTopics)
        {
            var forum = await context.Forums
                .FirstOrDefaultAsync(f => f.Name == starterTopic.ForumName);

            if (forum is null || await context.ForumTopics.AnyAsync(t => t.ForumId == forum.Id && t.Title == starterTopic.Title))
            {
                continue;
            }

            var topic = new ForumTopic
            {
                ForumId = forum.Id,
                UserId = adminUser.Id,
                Title = starterTopic.Title,
                NormalizedTitle = starterTopic.Title.Trim().ToUpperInvariant(),
                Content = starterTopic.Content,
                CreatedAt = DateTime.UtcNow
            };

            topic.Posts.Add(new ForumPost
            {
                UserId = adminUser.Id,
                Content = starterTopic.Content,
                CreatedAt = topic.CreatedAt
            });

            context.ForumTopics.Add(topic);
        }

        // Every book should have its own discussion forum in Community, created
        // automatically instead of waiting for someone to click "Book Discussion".
        var booksWithoutForum = await context.Books
            .Where(b => !context.Forums.Any(f => f.BookId == b.Id))
            .ToListAsync();

        foreach (var book in booksWithoutForum)
        {
            var forumName = $"{book.Title} Discussion";
            context.Forums.Add(new Forum
            {
                Name = forumName,
                NormalizedName = forumName.Trim().ToUpperInvariant(),
                Description = $"Discuss {book.Title} with other readers.",
                BookId = book.Id
            });
        }

        await context.SaveChangesAsync();
    }
}
