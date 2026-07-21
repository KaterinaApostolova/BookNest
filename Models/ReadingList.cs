using System.ComponentModel.DataAnnotations;

namespace BookNest.Models;

public class ReadingList
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public int BookId { get; set; }

    public Book? Book { get; set; }

    public int ReadingStatusId { get; set; }

    public ReadingStatus? ReadingStatus { get; set; }
}
