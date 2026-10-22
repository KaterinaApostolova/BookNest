using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookNest.ViewModels;

public class EditProfileViewModel
{
    [Required(ErrorMessage = "Please enter your Full Name.")]
    [StringLength(100, ErrorMessage = "Full Name must be 100 characters or fewer.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your Username.")]
    [StringLength(50, ErrorMessage = "Username must be 50 characters or fewer.")]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your Email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Bio must be 500 characters or fewer.")]
    [Display(Name = "Bio")]
    [DataType(DataType.MultilineText)]
    public string? Bio { get; set; }

    [Display(Name = "Public Profile")]
    public bool IsProfilePublic { get; set; }

    [Url(ErrorMessage = "Please enter a valid image URL.")]
    [StringLength(500)]
    [Display(Name = "Profile Picture")]
    public string? ProfilePictureUrl { get; set; }

    // Favourite genre
    [StringLength(100)]
    [Display(Name = "Favourite Genre")]
    public string? FavouriteGenre { get; set; }

    public IEnumerable<SelectListItem> Genres { get; set; }
        = new List<SelectListItem>();

    // Favourite book
    [StringLength(200)]
    [Display(Name = "Favourite Genre")]
    public string? FavouriteBook { get; set; }

    [StringLength(1000, ErrorMessage = "Favourite quote must be 1000 characters or fewer.")]
    [Display(Name = "Favourite Quote")]
    [DataType(DataType.MultilineText)]
    public string? FavouriteQuote { get; set; }

    public IEnumerable<SelectListItem> Books { get; set; }
        = new List<SelectListItem>();

    // Password
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string? CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    [StringLength(
        100,
        MinimumLength = 6,
        ErrorMessage = "Password must be at least 6 characters long.")]
    [Display(Name = "New Password")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare(
        nameof(Password),
        ErrorMessage = "Passwords do not match.")]
    public string? ConfirmPassword { get; set; }

    public DateTime MemberSince { get; set; }
}