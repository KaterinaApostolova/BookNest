using Microsoft.AspNetCore.Identity;

namespace BookNest.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public string? Bio { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ReadingList> ReadingLists { get; set; } = new List<ReadingList>();

    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
