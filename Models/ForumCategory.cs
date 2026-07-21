using System.ComponentModel.DataAnnotations;

namespace BookNest.Models;

public class ForumCategory
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public ICollection<ForumTopic> Topics { get; set; } = new List<ForumTopic>();
}
