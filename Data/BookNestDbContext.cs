using BookNest.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Data;

public class BookNestDbContext : IdentityDbContext<ApplicationUser>
{
    public BookNestDbContext(DbContextOptions<BookNestDbContext> options)
        : base(options)
    {
    }

    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookLink> BookLinks => Set<BookLink>();
    public DbSet<BookDiscussion> BookDiscussions => Set<BookDiscussion>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ForumCategory> ForumCategories => Set<ForumCategory>();
    public DbSet<ForumPost> ForumPosts => Set<ForumPost>();
    public DbSet<ForumTopic> ForumTopics => Set<ForumTopic>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<ReadingList> ReadingLists => Set<ReadingList>();
    public DbSet<ReadingStatus> ReadingStatuses => Set<ReadingStatus>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Book>()
            .HasOne(b => b.Author)
            .WithMany(a => a.Books)
            .HasForeignKey(b => b.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Book>()
            .HasOne(b => b.Genre)
            .WithMany(g => g.Books)
            .HasForeignKey(b => b.GenreId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Book>()
            .HasOne(b => b.Language)
            .WithMany(l => l.Books)
            .HasForeignKey(b => b.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadingList>()
            .HasOne(r => r.Book)
            .WithMany(b => b.ReadingLists)
            .HasForeignKey(r => r.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReadingList>()
            .HasOne(r => r.ReadingStatus)
            .WithMany(s => s.ReadingLists)
            .HasForeignKey(r => r.ReadingStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Book)
            .WithMany(b => b.Reviews)
            .HasForeignKey(r => r.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Rating>()
            .HasOne(r => r.Book)
            .WithMany()
            .HasForeignKey(r => r.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ForumPost>()
            .HasOne(p => p.ForumTopic)
            .WithMany(t => t.Posts)
            .HasForeignKey(p => p.ForumTopicId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Conversation>()
            .HasOne(c => c.BookDiscussion)
            .WithMany(d => d.Conversations)
            .HasForeignKey(c => c.BookDiscussionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
