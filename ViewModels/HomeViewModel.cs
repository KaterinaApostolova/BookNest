using BookNest.Models;

namespace BookNest.ViewModels;

public class HomeViewModel
{
    public IEnumerable<Book> FeaturedBooks { get; set; } = Enumerable.Empty<Book>();

    public IEnumerable<Review> RecentReviews { get; set; } = Enumerable.Empty<Review>();
}
