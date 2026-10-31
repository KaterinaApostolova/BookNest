using System.ComponentModel.DataAnnotations;

namespace BookNest.ViewModels;

public class ReviewViewModel
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public string BookTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a rating.")]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }

    [Required(ErrorMessage = "Please write your review.")]
    [StringLength(9000, ErrorMessage = "Review cannot exceed 9000 characters.")]
    public string Content { get; set; } = string.Empty;

    public string? UserId { get; set; }

    public string? Username { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsAnonymous { get; set; }
}
