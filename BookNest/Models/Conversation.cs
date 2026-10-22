using System.ComponentModel.DataAnnotations;

namespace BookNest.Models;

public class Conversation
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Subject { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? BookDiscussionId { get; set; }

    public BookDiscussion? BookDiscussion { get; set; }

    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    public int? BookId { get; set; }

    public Book? Book { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
