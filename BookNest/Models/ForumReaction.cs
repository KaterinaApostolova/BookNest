namespace BookNest.Models;

public class ForumReaction
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public int? ForumTopicId { get; set; }

    public ForumTopic? ForumTopic { get; set; }

    public int? ForumPostId { get; set; }

    public ForumPost? ForumPost { get; set; }

    /// <summary>true = like, false = dislike.</summary>
    public bool IsLike { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
