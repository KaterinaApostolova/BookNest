using BookNest.Data;
using BookNest.Models;
using BookNest.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Services;

public class HomeViewService
{
    private readonly BookNestDbContext _context;

    public HomeViewService(BookNestDbContext context)
    {
        _context = context;
    }

    public async Task<HomeViewModel> GetHomeViewModelAsync()
    {
        var featuredBooks = await _context.Books
            .Include(b => b.Author)
            .Include(b => b.Genre)
            .OrderBy(b => b.Title)
            .Take(3)
            .ToListAsync();

        var recentReviews = await _context.Reviews
            .Include(r => r.Book)
            .OrderByDescending(r => r.CreatedAt)
            .Take(3)
            .ToListAsync();

        return new HomeViewModel
        {
            FeaturedBooks = featuredBooks,
            RecentReviews = recentReviews
        };
    }
}
