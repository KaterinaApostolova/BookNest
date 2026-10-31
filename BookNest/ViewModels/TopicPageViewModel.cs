using BookNest.Models;

namespace BookNest.ViewModels;

public class TopicPageViewModel
{
    public ForumTopic Topic { get; set; } = null!;
    public List<ForumPost> Posts { get; set; } = new();
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}
