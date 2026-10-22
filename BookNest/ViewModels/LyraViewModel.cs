using System.ComponentModel.DataAnnotations;

namespace BookNest.ViewModels;

public class LyraViewModel
{
    public int? ConversationId { get; set; }

    public int? BookId { get; set; }

    [Display(Name = "Your message")]
    [Required(ErrorMessage = "Please enter a message for Lyra.")]
    public string Message { get; set; } = string.Empty;

    public List<LyraConversationViewModel> Conversations { get; set; } = new();

    public List<LyraMessageViewModel> Messages { get; set; } = new();
}

public class LyraConversationViewModel
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

public class LyraMessageViewModel
{
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsUser { get; set; }
}