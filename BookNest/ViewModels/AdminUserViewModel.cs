namespace BookNest.ViewModels;
public class AdminUserViewModel
{
    public string Id { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime MemberSince { get; set; }

    public bool IsProfilePublic { get; set; }

    public bool IsDeactivated { get; set; }

    public bool IsAdmin { get; set; }

    public int BooksRead { get; set; }

    public int CurrentlyReading { get; set; }

    public int WantToRead { get; set; }

    public int ReviewsWritten { get; set; }
}
