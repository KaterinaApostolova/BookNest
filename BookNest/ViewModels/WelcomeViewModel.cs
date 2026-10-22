namespace BookNest.ViewModels;

public class WelcomeViewModel
{
    public string Title { get; set; } = "Welcome to BookNest.";
    public string? Subtitle { get; set; }
    public string Quote { get; set; } = "Welcome to BookNest.";
    public int RedirectDelayMilliseconds { get; set; } = 3000;
}
