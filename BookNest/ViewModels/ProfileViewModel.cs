namespace BookNest.ViewModels;

public class ProfileViewModel
{

    // Profile information
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AvatarInitials { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    public DateTime MemberSince { get; set; }
    public bool IsProfilePublic { get; set; }
    // Favourite information
    public string? FavouriteGenre { get; set; }
    public string? FavouriteBookTitle { get; set; }
    public string? FavouriteQuote { get; set; }
    // Reading statistics
    public int BooksRead { get; set; }
    public int CurrentlyReading { get; set; }
    public int WantToRead { get; set; }
    // Reviews
    public int ReviewsWritten { get; set; }
    public List<ProfileReviewViewModel> Reviews { get; set; } = new();
    // Contribution requests
    public List<ProfileContributionRequestViewModel> ContributionRequests { get; set; } = new();
    // Community statistics
    public int TopicsStarted { get; set; }
    public int RepliesWritten { get; set; }
    public int ForumsStarted { get; set; }
}