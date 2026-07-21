using System.ComponentModel.DataAnnotations;

namespace BookNest.Models;

public class Book
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public int AuthorId { get; set; }

    public Author? Author { get; set; }

    [Required]
    public int GenreId { get; set; }

    public Genre? Genre { get; set; }

    [Required]
    public int LanguageId { get; set; }

    public Language? Language { get; set; }

    [DataType(DataType.Date)]
    public DateTime? PublishedDate { get; set; }

    [StringLength(200)]
    public string? CoverImageUrl { get; set; }

    public ICollection<BookLink> Links { get; set; } = new List<BookLink>();

    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    public ICollection<ReadingList> ReadingLists { get; set; } = new List<ReadingList>();

    public ICollection<BookDiscussion> Discussions { get; set; } = new List<BookDiscussion>();
}
