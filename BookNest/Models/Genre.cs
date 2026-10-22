using System.ComponentModel.DataAnnotations;

namespace BookNest.Models;

public class Genre
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Book> Books { get; set; } = new List<Book>();

    public ICollection<BookGenre> BookGenres { get; set; } = new List<BookGenre>();
}
