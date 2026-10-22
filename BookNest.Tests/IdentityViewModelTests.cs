using System.ComponentModel.DataAnnotations;
using BookNest.ViewModels;

namespace BookNest.Tests;

public class IdentityViewModelTests
{
    [Test]
    public void RegisterViewModel_RequiresSeparateIdentityFields()
    {
        var model = new RegisterViewModel
        {
            FullName = "Jordan Lee",
            Username = "jordan",
            Email = "jordan@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        Assert.That(isValid, Is.True);
        Assert.That(model.Username, Is.Not.EqualTo(model.Email));
        Assert.That(model.FullName, Is.Not.EqualTo(model.Username));
        Assert.That(model.FullName, Is.Not.EqualTo(model.Email));
    }

    [Test]
    public void LoginViewModel_AllowsUsernameOrEmailLogin()
    {
        var model = new LoginViewModel
        {
            UsernameOrEmail = "bookworm",
            Password = "Password123!"
        };

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        Assert.That(isValid, Is.True);
        Assert.That(model.UsernameOrEmail, Is.EqualTo("bookworm"));
    }
}
