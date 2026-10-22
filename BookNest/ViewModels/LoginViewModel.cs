using System.ComponentModel.DataAnnotations;

namespace BookNest.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your Username or Email.")]
    [Display(Name = "Username or Email")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your Password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
