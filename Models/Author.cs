using System.ComponentModel.DataAnnotations;

namespace BookNest.Models;

public class Author
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Biography { get; set; }

    public ICollection<Book> Books { get; set; } = new List<Book>();
}
