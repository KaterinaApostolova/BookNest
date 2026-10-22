using System.ComponentModel.DataAnnotations;

namespace BookNest.Models;

public class ContributionRequest
{
    public int Id { get; set; }

    public int? BookId { get; set; }

    public Book? Book { get; set; }

    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    [Required]
    [StringLength(50)]
    public string RequestType { get; set; } = string.Empty;

    [Required]
    [StringLength(3000)]
    public string Message { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? AdditionalInformation { get; set; }

    [StringLength(500)]
    [Url]
    public string? SourceUrl { get; set; }

    // Used only for ReadingLink requests
    [StringLength(100)]
    public string? WebsiteName { get; set; }

    [StringLength(100)]
    public string? Language { get; set; }

    [StringLength(50)]
    public string? LinkType { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = ContributionRequestStatuses.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

