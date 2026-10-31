using System.ComponentModel.DataAnnotations;

namespace BookNest.Models;

public class ForumPost
{
    public int Id { get; set; }

    [Required]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int ForumTopicId { get; set; }

    public ForumTopic? ForumTopic { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public bool IsAnonymous { get; set; }

    public DateTime? EditedAt { get; set; }

    public ICollection<ForumReaction> Reactions { get; set; } = new List<ForumReaction>();
}
