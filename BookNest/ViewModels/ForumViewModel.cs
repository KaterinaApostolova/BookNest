using System.ComponentModel.DataAnnotations;

namespace BookNest.ViewModels;

public class ForumViewModel
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}