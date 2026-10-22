using System.ComponentModel.DataAnnotations;

namespace BookNest.ViewModels;

public class BookFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Please enter the book title.")]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Publication Year")]
    public int? PublicationYear { get; set; }

    [Display(Name = "Cover Image URL")]
    public string? CoverImageUrl { get; set; }

    [Range(1, 5, ErrorMessage = "Average Rating must be between 1 and 5.")]
    [Display(Name = "Average Rating")]
    public double? AverageRating { get; set; }

    [Required(ErrorMessage = "Please enter at least one author.")]
    [Display(Name = "Authors")]
    public string Authors { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter at least one genre.")]
    [Display(Name = "Genres")]
    public string Genres { get; set; } = string.Empty;
    public List<ReadingLinkViewModel> ReadingLinks { get; set; } = new();
}
