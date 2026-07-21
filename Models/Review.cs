using System.ComponentModel.DataAnnotations;

namespace BookNest.Models;

public class Review
{
    public int Id { get; set; }

    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int BookId { get; set; }

    public Book? Book { get; set; }

    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }
}
