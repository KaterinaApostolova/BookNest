namespace BookNest.ViewModels
{
    public class ProfileReviewViewModel
    {
        public int Id { get; set; }

        public int BookId { get; set; }

        public string BookTitle { get; set; } = string.Empty;

        public int Rating { get; set; }

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}