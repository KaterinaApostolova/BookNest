using System.ComponentModel.DataAnnotations;

namespace BookNest.Models;

public class BookDiscussion
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int BookId { get; set; }

    public Book? Book { get; set; }

    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}
