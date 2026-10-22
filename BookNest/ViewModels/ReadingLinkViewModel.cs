using System.ComponentModel.DataAnnotations;

namespace BookNest.ViewModels
{
    public class ReadingLinkViewModel
    {
        public int Id { get; set; }

        public int BookId { get; set; }

        [Required(ErrorMessage = "Please enter the website name.")]
        [StringLength(100)]
        public string WebsiteName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the language.")]
        [StringLength(100)]
        public string Language { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the link.")]
        [Url(ErrorMessage = "Please enter a valid URL.")]
        public string Url { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a link type.")]
        [StringLength(50)]
        public string LinkType { get; set; } = string.Empty;

    }
}