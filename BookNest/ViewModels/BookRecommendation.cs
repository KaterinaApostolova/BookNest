using BookNest.Models;

namespace BookNest.ViewModels;

public class BookRecommendation
{
    public Book Book { get; set; } = null!;
    public string Reason { get; set; } = string.Empty;
}
