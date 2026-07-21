using System.Diagnostics;
using BookNest.Models;
using BookNest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookNest.Controllers;

public class HomeController : Controller
{
    private readonly HomeViewService _homeViewService;

    public HomeController(HomeViewService homeViewService)
    {
        _homeViewService = homeViewService;
    }

    public async Task<IActionResult> Index()
    {
        var model = await _homeViewService.GetHomeViewModelAsync();
        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Admin()
    {
        return View("Privacy");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
