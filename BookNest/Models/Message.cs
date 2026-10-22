using System.ComponentModel.DataAnnotations;

namespace BookNest.Models;

public class Message
{
    public int Id { get; set; }

    [Required]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public int ConversationId { get; set; }

    public Conversation? Conversation { get; set; }

    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }
}
