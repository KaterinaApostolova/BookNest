using System.ComponentModel.DataAnnotations;

namespace BookNest.Models;

public class ForumTopic
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int ForumCategoryId { get; set; }

    public ForumCategory? ForumCategory { get; set; }

    public ICollection<ForumPost> Posts { get; set; } = new List<ForumPost>();
}
