using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BookNest.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Bio { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? ProfilePictureUrl { get; set; }

    public bool IsProfilePublic { get; set; } = true;

    // Favourite profile information
    [StringLength(200)]
    public string? FavouriteBook { get; set; }

    [StringLength(100)]
    public string? FavouriteGenre { get; set; }

    [StringLength(1000)]
    public string? FavouriteQuote { get; set; }

    public ICollection<ReadingList> ReadingLists { get; set; }
        = new List<ReadingList>();

    public ICollection<Review> Reviews { get; set; }
        = new List<Review>();

    public ICollection<Conversation> Conversations { get; set; }
        = new List<Conversation>();

    public ICollection<Message> Messages { get; set; }
        = new List<Message>();

    public ICollection<ForumTopic> ForumTopics { get; set; }
        = new List<ForumTopic>();

    public ICollection<ForumPost> ForumPosts { get; set; }
        = new List<ForumPost>();

    public bool IsDeactivated { get; set; } = false;
}
