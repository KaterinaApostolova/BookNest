using System.ComponentModel.DataAnnotations;

namespace BookNest.ViewModels;

public class PublicReviewViewModel
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public string BookTitle { get; set; } = string.Empty;

    public string? BookCoverUrl { get; set; }

    public int Rating { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}