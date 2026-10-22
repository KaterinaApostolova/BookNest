using System.ComponentModel.DataAnnotations;

namespace BookNest.Models;

public class ForumTopic
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string NormalizedTitle { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Content { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int ForumId { get; set; }

    public Forum? Forum { get; set; }

    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    public ICollection<ForumPost> Posts { get; set; } = new List<ForumPost>();
}
