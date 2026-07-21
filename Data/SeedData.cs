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

        if (!await context.Languages.AnyAsync())
        {
            context.Languages.AddRange(
                new Language { Name = "English" },
                new Language { Name = "Bulgarian" });
        }

        if (!await context.ReadingStatuses.AnyAsync())
        {
            context.ReadingStatuses.AddRange(
                new ReadingStatus { Name = "Want to Read" },
                new ReadingStatus { Name = "Reading" },
                new ReadingStatus { Name = "Completed" });
        }

        if (!await context.ForumCategories.AnyAsync())
        {
            context.ForumCategories.AddRange(
                new ForumCategory { Name = "General Talk", Description = "Share your reading thoughts" });
        }

        if (!await context.Authors.AnyAsync())
        {
            context.Authors.Add(new Author { Name = "Jane Austen", Biography = "A beloved classic author." });
            context.Authors.Add(new Author { Name = "Octavia Butler", Biography = "A visionary science fiction writer." });
        }

        if (!await context.Books.AnyAsync())
        {
            var authors = await context.Authors.ToListAsync();
            var genres = await context.Genres.ToListAsync();
            var languages = await context.Languages.ToListAsync();

            var author1 = authors.FirstOrDefault(a => a.Name == "Jane Austen");
            var author2 = authors.FirstOrDefault(a => a.Name == "Octavia Butler");
            var genreFantasy = genres.FirstOrDefault(g => g.Name == "Fantasy");
            var genreSciFi = genres.FirstOrDefault(g => g.Name == "Science Fiction");
            var english = languages.FirstOrDefault(l => l.Name == "English");

            if (author1 is not null && author2 is not null && genreFantasy is not null && genreSciFi is not null && english is not null)
            {
                context.Books.AddRange(
                    new Book
                    {
                        Title = "Pride and Prejudice",
                        Description = "A timeless story about first impressions and personal growth.",
                        AuthorId = author1.Id,
                        GenreId = genreFantasy.Id,
                        LanguageId = english.Id,
                        PublishedDate = new DateTime(1813, 1, 28)
                    },
                    new Book
                    {
                        Title = "Kindred",
                        Description = "A powerful story that blends history and imagination.",
                        AuthorId = author2.Id,
                        GenreId = genreSciFi.Id,
                        LanguageId = english.Id,
                        PublishedDate = new DateTime(1979, 1, 1)
                    });
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
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "BookNest Admin"
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
        else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        await context.SaveChangesAsync();
    }
}
