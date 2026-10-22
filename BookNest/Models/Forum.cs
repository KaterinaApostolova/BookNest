using System.ComponentModel.DataAnnotations;

namespace BookNest.Models;

public class Forum
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string NormalizedName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public int? BookId { get; set; }

    public Book? Book { get; set; }

    public ICollection<ForumTopic> Topics { get; set; } = new List<ForumTopic>();
}