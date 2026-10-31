using System.Diagnostics;
using System.Security.Claims;
using BookNest.ViewModels;
using BookNest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookNest.Controllers;

public class HomeController : Controller
{
    private readonly HomeViewService _homeViewService;
    private readonly IBookRecommendationService _recommendationService;

    public HomeController(HomeViewService homeViewService, IBookRecommendationService recommendationService)
    {
        _homeViewService = homeViewService;
        _recommendationService = recommendationService;
    }

    public async Task<IActionResult> Index()
    {
        var model = await _homeViewService.GetHomeViewModelAsync();
        return View(model);
    }

    // GET: Home/Recommendations — loaded lazily via AJAX so a slow AI call never
    // delays the homepage itself for any visitor.
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Recommendations(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return PartialView("_Recommendations", new List<BookRecommendation>());
        }

        var recommendations = await _recommendationService.GetRecommendationsAsync(userId, cancellationToken);
        return PartialView("_Recommendations", recommendations);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
