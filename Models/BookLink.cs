using System.ComponentModel.DataAnnotations;

namespace BookNest.Models;

public class BookLink
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;

    public int BookId { get; set; }

    public Book? Book { get; set; }
}
