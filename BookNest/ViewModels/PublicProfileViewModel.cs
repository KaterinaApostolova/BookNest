namespace BookNest.ViewModels;

public class PublicProfileViewModel
{
    public string DisplayName { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;
    public string? FullName { get; set; }

    public string AvatarInitials { get; set; } = string.Empty;

    public string? ProfilePictureUrl { get; set; }

    public string? Bio { get; set; }

    public DateTime MemberSince { get; set; }

    public string? FavouriteGenre { get; set; }

    public string? FavouriteBook { get; set; }
    public string? FavouriteQuote { get; set; }

    public int BooksRead { get; set; }

    public int CurrentlyReading { get; set; }

    public int WantToRead { get; set; }

    public int ReviewsWritten { get; set; }

    public int BooksInJourney { get; set; }

    public int TopicsStarted { get; set; }

    public int RepliesWritten { get; set; }

    public int ForumsStarted { get; set; }

    public bool IsOwnProfile { get; set; }

    public List<PublicReviewViewModel> Reviews { get; set; }
        = new();
}