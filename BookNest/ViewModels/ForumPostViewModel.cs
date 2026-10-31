using System.ComponentModel.DataAnnotations;

namespace BookNest.ViewModels;

public class ForumPostViewModel
{
    public int Id { get; set; }

    public int ForumTopicId { get; set; }

    [Required]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;

    public bool IsAnonymous { get; set; }
}