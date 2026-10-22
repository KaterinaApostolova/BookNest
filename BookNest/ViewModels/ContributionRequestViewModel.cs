using System.ComponentModel.DataAnnotations;

namespace BookNest.ViewModels;

public class ContributionRequestViewModel
{
    public int? BookId { get; set; }

    public string BookTitle { get; set; } = string.Empty;

    [Required]
    public string RequestType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please describe your suggestion.")]
    [StringLength(3000, ErrorMessage = "Your suggestion cannot exceed 3000 characters.")]
    [Display(Name = "Suggestion")]
    public string Message { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Additional Information")]
    public string? AdditionalInformation { get; set; }

    [Url(ErrorMessage = "Please enter a valid URL.")]
    [StringLength(500)]
    [Display(Name = "Source / Link")]
    public string? SourceUrl { get; set; }

    // Reading link only

    [StringLength(100)]
    [Display(Name = "Website")]
    public string? WebsiteName { get; set; }

    [StringLength(100)]
    [Display(Name = "Language")]
    public string? Language { get; set; }

    [StringLength(50)]
    [Display(Name = "Link Type")]
    public string? LinkType { get; set; }
}