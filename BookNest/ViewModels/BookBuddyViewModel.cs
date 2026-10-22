using System.ComponentModel.DataAnnotations;

namespace BookNest.ViewModels;

public class BookBuddyViewModel
{
    [StringLength(500)]
    public string UserMessage { get; set; } = string.Empty;

    public List<BookBuddyMessageViewModel> Messages { get; set; } = new();
}

public class BookBuddyMessageViewModel
{
    public string Text { get; set; } = string.Empty;

    public bool IsUser { get; set; }
}