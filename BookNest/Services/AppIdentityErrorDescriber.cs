using Microsoft.AspNetCore.Identity;

namespace BookNest.Services;

public class AppIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DuplicateEmail(string? email) => new()
    {
        Code = nameof(DuplicateEmail),
        Description = "This email address is already registered."
    };

    public override IdentityError DuplicateUserName(string? userName) => new()
    {
        Code = nameof(DuplicateUserName),
        Description = "This username is already taken."
    };

    public override IdentityError InvalidEmail(string? email) => new()
    {
        Code = nameof(InvalidEmail),
        Description = "Please enter a valid email address."
    };

    public override IdentityError PasswordTooShort(int length) => new()
    {
        Code = nameof(PasswordTooShort),
        Description = "Password must be at least 6 characters long."
    };

    public override IdentityError PasswordRequiresNonAlphanumeric() => new()
    {
        Code = nameof(PasswordRequiresNonAlphanumeric),
        Description = "Password must include at least one symbol."
    };

    public override IdentityError PasswordRequiresDigit() => new()
    {
        Code = nameof(PasswordRequiresDigit),
        Description = "Password must include at least one numeric digit."
    };

    public override IdentityError PasswordRequiresLower() => new()
    {
        Code = nameof(PasswordRequiresLower),
        Description = "Password must include at least one lowercase letter."
    };

    public override IdentityError PasswordRequiresUpper() => new()
    {
        Code = nameof(PasswordRequiresUpper),
        Description = "Password must include at least one uppercase letter."
    };
}
