using System.ComponentModel.DataAnnotations;

namespace BookNest.ViewModels;

public class ResetPasswordViewModel
{
    public string UserId { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a new password.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
