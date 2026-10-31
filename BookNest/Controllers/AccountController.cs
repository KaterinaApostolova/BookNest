using BookNest.Data;
using BookNest.Models;
using BookNest.Services;
using BookNest.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly QuoteService _quoteService;
    private readonly IEmailSender _emailSender;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        QuoteService quoteService,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _quoteService = quoteService;
        _emailSender = emailSender;
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
       
        var user = new ApplicationUser
        {
            FullName = model.FullName,
            UserName = model.Username,
            Email = model.Email,
            Bio = string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "User");
            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData["WelcomeType"] = "register";
            TempData["WelcomeUserName"] = user.FullName ?? user.UserName;
            return RedirectToAction(nameof(Welcome));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
    LoginViewModel model,
    string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Find user by username or email
        var user =
            await _userManager.FindByNameAsync(model.UsernameOrEmail)
            ?? await _userManager.FindByEmailAsync(model.UsernameOrEmail);


        // User does not exist
        if (user is null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Invalid login attempt.");

            return View(model);
        }


        // Check whether the account has been deactivated
        if (user.IsDeactivated)
        {
            ModelState.AddModelError(
                string.Empty,
                "This account has been deactivated by an administrator.");

            return View(model);
        }


        // Only active users reach this point
        var result =
            await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);


        if (result.Succeeded)
        {
            TempData["WelcomeType"] = "login";

            TempData["WelcomeUserName"] =
                user.FullName ??
                user.UserName ??
                model.UsernameOrEmail;

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                TempData["WelcomeRedirectUrl"] = returnUrl;
            }

            return RedirectToAction(nameof(Welcome));
        }


        ModelState.AddModelError(
            string.Empty,
            "Invalid login attempt.");

        return View(model);
    }

    [HttpGet]
    public IActionResult Welcome()
    {
        var welcomeType = TempData["WelcomeType"] as string;
        if (string.IsNullOrWhiteSpace(welcomeType))
        {
            return RedirectToAction("Index", "Home");
        }

        var userName = TempData["WelcomeUserName"] as string;
        var redirectUrl = TempData["WelcomeRedirectUrl"] as string;
        var safeRedirectUrl = !string.IsNullOrWhiteSpace(redirectUrl) && Url.IsLocalUrl(redirectUrl)
            ? redirectUrl
            : Url.Action("Index", "Home") ?? "/";

        var isBulgarian = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "bg";

        var model = welcomeType switch
        {
            "register" => new WelcomeViewModel
            {
                Title = isBulgarian ? "Добре дошъл в BookNest." : "Welcome to BookNest.",
                Subtitle = isBulgarian ? "Защото никоя история не бива да свършва с последната страница." : "Because no story should end with the last page.",
                Quote = _quoteService.GetQuote(QuoteCollection.Register),
                RedirectDelayMilliseconds = 7000,
                RedirectUrl = safeRedirectUrl
            },
            "login" => new WelcomeViewModel
            {
                Title = isBulgarian ? $"Добре дошъл отново, {userName ?? "читателю"}!" : $"Welcome back, {userName ?? "reader"}!",
                Quote = _quoteService.GetQuote(QuoteCollection.Login),
                RedirectDelayMilliseconds = 7000,
                RedirectUrl = safeRedirectUrl
            },
            "logout" => new WelcomeViewModel
            {
                Title = isBulgarian ? "До скоро." : "See you soon.",
                Quote = _quoteService.GetQuote(QuoteCollection.Logout),
                RedirectDelayMilliseconds = 7000,
                RedirectUrl = safeRedirectUrl
            },
            _ => new WelcomeViewModel
            {
                Title = isBulgarian ? "Добре дошъл в BookNest." : "Welcome to BookNest.",
                Quote = _quoteService.GetQuote(QuoteCollection.Register),
                RedirectDelayMilliseconds = 7000,
                RedirectUrl = safeRedirectUrl
            }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["WelcomeType"] = "logout";
        return RedirectToAction(nameof(Welcome));
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);

        // Always show the same message, whether or not the account exists —
        // this avoids leaking which emails are registered on BookNest.
        if (user is not null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = System.Net.WebUtility.UrlEncode(token);
            var resetUrl = Url.Action(
                nameof(ResetPassword),
                "Account",
                new { userId = user.Id, token = encodedToken },
                protocol: Request.Scheme);

            var htmlBody = $"""
                <p>Hello {System.Net.WebUtility.HtmlEncode(user.FullName ?? user.UserName)},</p>
                <p>We received a request to reset your BookNest password. Click the link below to choose a new one:</p>
                <p><a href="{resetUrl}">Reset your password</a></p>
                <p>If you didn't request this, you can safely ignore this email — your password will stay the same.</p>
                <p>— BookNest</p>
                """;

            await _emailSender.SendEmailAsync(user.Email!, "Reset your BookNest password", htmlBody);
        }

        TempData["InfoMessage"] = "If an account exists for that email, a password reset link has been sent.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult ResetPassword(string? userId, string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new ResetPasswordViewModel { UserId = userId, Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user is null)
        {
            // Don't reveal whether the account exists.
            TempData["InfoMessage"] = "Your password has been reset.";
            return RedirectToAction(nameof(Login));
        }

        var decodedToken = System.Net.WebUtility.UrlDecode(model.Token);
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        TempData["InfoMessage"] = "Your password has been reset. You can now log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

}
