using BookNest.Models;
using BookNest.Services;
using BookNest.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookNest.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly QuoteService _quoteService;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        QuoteService quoteService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _quoteService = quoteService;
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
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName
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
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            TempData["WelcomeType"] = "login";
            TempData["WelcomeUserName"] = user?.FullName ?? user?.UserName ?? model.Email;
            return RedirectToAction(nameof(Welcome));
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
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
        var model = welcomeType switch
        {
            "register" => new WelcomeViewModel
            {
                Title = "Welcome to BookNest.",
                Subtitle = "Because no story should end with the last page.",
                Quote = _quoteService.GetQuote(QuoteCollection.Register),
                RedirectDelayMilliseconds = 3000
            },
            "login" => new WelcomeViewModel
            {
                Title = $"Welcome back, {userName ?? "reader"}!",
                Quote = _quoteService.GetQuote(QuoteCollection.Login),
                RedirectDelayMilliseconds = 2500
            },
            "logout" => new WelcomeViewModel
            {
                Title = "See you soon.",
                Quote = _quoteService.GetQuote(QuoteCollection.Logout),
                RedirectDelayMilliseconds = 2000
            },
            _ => new WelcomeViewModel
            {
                Title = "Welcome to BookNest.",
                Quote = _quoteService.GetQuote(QuoteCollection.Register),
                RedirectDelayMilliseconds = 3000
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
        if (user is null)
        {
            TempData["InfoMessage"] = "If an account exists for this email, a password reset link would be sent. Email is not configured in this demo setup.";
            return RedirectToAction(nameof(Login));
        }

        TempData["InfoMessage"] = "Password reset is prepared. Email delivery is not configured in this demo, so no message was sent.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    public IActionResult Profile() => View();
}
