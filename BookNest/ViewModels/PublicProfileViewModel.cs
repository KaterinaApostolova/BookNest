namespace BookNest.ViewModels;

public class PublicProfileViewModel
{
    public string DisplayName { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public string AvatarInitials { get; set; } = string.Empty;

    public string? ProfilePictureUrl { get; set; }

    public string? Bio { get; set; }

    public DateTime MemberSince { get; set; }

    public string? FavouriteGenre { get; set; }

    public string? FavouriteBook { get; set; }

    public int BooksRead { get; set; }

    public int ReviewsWritten { get; set; }

    public int BooksInJourney { get; set; }

    public List<PublicReviewViewModel> Reviews { get; set; }
        = new();
}