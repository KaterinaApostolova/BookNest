using System.ComponentModel.DataAnnotations;

namespace BookNest.Models;

public class Review
{
    public int Id { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }

    [Required]
    [StringLength(9000, ErrorMessage = "Review cannot exceed 9000 characters.")]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int BookId { get; set; }

    public Book? Book { get; set; }

    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }
}
