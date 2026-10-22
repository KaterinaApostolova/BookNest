using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookNest.Models;

public class Book
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(1900, 2100)]
    public int? PublicationYear { get; set; }

    [StringLength(200)]
    public string? CoverImageUrl { get; set; }

    [Range(1, 5)]
    public double? AverageRating { get; set; }

    public ICollection<Author> Authors { get; set; } = new List<Author>();

    public ICollection<Genre> Genres { get; set; } = new List<Genre>();

    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();

    public ICollection<BookGenre> BookGenres { get; set; } = new List<BookGenre>();

    [NotMapped]
    public Author? Author => Authors.FirstOrDefault();

    [NotMapped]
    public Genre? Genre => Genres.FirstOrDefault();

    public ICollection<ReadingLink> ReadingLinks { get; set; } = new List<ReadingLink>();

    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    public ICollection<ReadingList> ReadingLists { get; set; } = new List<ReadingList>();

    public ICollection<BookDiscussion> Discussions { get; set; } = new List<BookDiscussion>();

    public ICollection<Forum> Forums { get; set; } = new List<Forum>();
}
