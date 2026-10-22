namespace BookNest.ViewModels
{
    public class ProfileContributionRequestViewModel
    {
        public int Id { get; set; }

        public string RequestType { get; set; } = string.Empty;

        public string DisplayType { get; set; } = string.Empty;

        public int? BookId { get; set; }

        public string? BookTitle { get; set; }

        public string Message { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}