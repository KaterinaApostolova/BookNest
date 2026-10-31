using BookNest.Models;

namespace BookNest.ViewModels;

public class ForumPageViewModel
{
    public Forum Forum { get; set; } = null!;
    public List<ForumTopic> Topics { get; set; } = new();
    public string Sort { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}
