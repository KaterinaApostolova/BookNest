namespace BookNest.ViewModels;

public class AuthorDisplayViewModel
{
    public string? UserName { get; set; }
    public bool IsProfilePublic { get; set; }
    public bool IsAnonymous { get; set; }
    public string FallbackLabel { get; set; } = "BookNest member";
    public string AnonymousLabel { get; set; } = "Anonymous";
    public string? AvatarUrl { get; set; }
    public int? PostCount { get; set; }
    public string PostsSuffix { get; set; } = "posts";
}
