using System.ComponentModel.DataAnnotations;

namespace BookNest.ViewModels;

public class ForumTopicViewModel
{
    public int Id { get; set; }

    public int ForumId { get; set; }

    public string ForumName { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    [DataType(DataType.MultilineText)]
    public string Content { get; set; } = string.Empty;

    public bool IsAnonymous { get; set; }
}