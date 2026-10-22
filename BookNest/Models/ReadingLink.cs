using System.ComponentModel.DataAnnotations;

namespace BookNest.Models;

public class ReadingLink
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Language { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string WebsiteName { get; set; } = string.Empty;

    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LinkType { get; set; } = string.Empty;

    public int BookId { get; set; }

    public Book? Book { get; set; }
}
