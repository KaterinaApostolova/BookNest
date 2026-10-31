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
    public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();
    public DbSet<BookGenre> BookGenres => Set<BookGenre>();
    public DbSet<ReadingLink> ReadingLinks => Set<ReadingLink>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ContributionRequest> ContributionRequests => Set<ContributionRequest>();
    public DbSet<Forum> Forums => Set<Forum>();
    public DbSet<ForumPost> ForumPosts => Set<ForumPost>();
    public DbSet<ForumTopic> ForumTopics => Set<ForumTopic>();
    public DbSet<ForumReaction> ForumReactions => Set<ForumReaction>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ReadingList> ReadingLists => Set<ReadingList>();
    public DbSet<ReadingStatus> ReadingStatuses => Set<ReadingStatus>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Book>()
            .HasMany(b => b.Authors)
            .WithMany(a => a.Books)
            .UsingEntity<BookAuthor>(
                ba => ba.HasOne(ba => ba.Author).WithMany(a => a.BookAuthors).HasForeignKey(ba => ba.AuthorId).OnDelete(DeleteBehavior.Cascade),
                ba => ba.HasOne(ba => ba.Book).WithMany(b => b.BookAuthors).HasForeignKey(ba => ba.BookId).OnDelete(DeleteBehavior.Cascade),
                ba => ba.HasKey(ba => new { ba.BookId, ba.AuthorId }));

        modelBuilder.Entity<Book>()
            .HasMany(b => b.Genres)
            .WithMany(g => g.Books)
            .UsingEntity<BookGenre>(
                bg => bg.HasOne(bg => bg.Genre).WithMany(g => g.BookGenres).HasForeignKey(bg => bg.GenreId).OnDelete(DeleteBehavior.Cascade),
                bg => bg.HasOne(bg => bg.Book).WithMany(b => b.BookGenres).HasForeignKey(bg => bg.BookId).OnDelete(DeleteBehavior.Cascade),
                bg => bg.HasKey(bg => new { bg.BookId, bg.GenreId }));

        modelBuilder.Entity<Author>()
            .HasIndex(a => a.Name)
            .IsUnique();

        modelBuilder.Entity<Genre>()
            .HasIndex(g => g.Name)
            .IsUnique();

        modelBuilder.Entity<ReadingLink>()
            .HasOne(r => r.Book)
            .WithMany(b => b.ReadingLinks)
            .HasForeignKey(r => r.BookId)
            .OnDelete(DeleteBehavior.Cascade);

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

        modelBuilder.Entity<ContributionRequest>()
            .HasOne(r => r.Book)
            .WithMany()
            .HasForeignKey(r => r.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ForumTopic>()
            .HasOne(t => t.Forum)
            .WithMany(f => f.Topics)
            .HasForeignKey(t => t.ForumId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Forum>()
            .HasOne(f => f.Book)
            .WithMany(b => b.Forums)
            .HasForeignKey(f => f.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Forum>()
            .HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Forum>()
            .HasIndex(f => f.NormalizedName)
            .IsUnique();

        modelBuilder.Entity<Forum>()
            .HasIndex(f => f.BookId)
            .IsUnique()
            .HasFilter("[BookId] IS NOT NULL");

        modelBuilder.Entity<ForumTopic>()
            .HasIndex(t => new { t.ForumId, t.NormalizedTitle })
            .IsUnique();

        modelBuilder.Entity<ForumTopic>()
            .HasOne(t => t.User)
            .WithMany(u => u.ForumTopics)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ForumReaction>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ForumReaction>()
            .HasOne(r => r.ForumTopic)
            .WithMany(t => t.Reactions)
            .HasForeignKey(r => r.ForumTopicId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ForumReaction>()
            .HasOne(r => r.ForumPost)
            .WithMany(p => p.Reactions)
            .HasForeignKey(r => r.ForumPostId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ForumReaction>()
            .HasIndex(r => new { r.UserId, r.ForumTopicId })
            .IsUnique()
            .HasFilter("[ForumTopicId] IS NOT NULL");

        modelBuilder.Entity<ForumReaction>()
            .HasIndex(r => new { r.UserId, r.ForumPostId })
            .IsUnique()
            .HasFilter("[ForumPostId] IS NOT NULL");

        modelBuilder.Entity<ForumPost>()
            .HasOne(p => p.ForumTopic)
            .WithMany(t => t.Posts)
            .HasForeignKey(p => p.ForumTopicId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ForumPost>()
            .HasOne(p => p.User)
            .WithMany(u => u.ForumPosts)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Conversation>()
            .HasOne(c => c.Book)
            .WithMany()
            .HasForeignKey(c => c.BookId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
