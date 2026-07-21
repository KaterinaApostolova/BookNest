using System.ComponentModel.DataAnnotations;

namespace BookNest.ViewModels;

public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
