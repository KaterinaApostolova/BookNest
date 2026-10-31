using BookNest.ViewModels;

namespace BookNest.Services;

public interface IBookRecommendationService
{
    /// <summary>
    /// Returns up to a handful of catalog books the given user hasn't already added to
    /// their Reading Journey, picked to match their taste. Returns an empty list if there
    /// isn't enough taste data yet, the catalog is too small, or the AI call fails —
    /// callers should simply hide the recommendations section in that case.
    /// </summary>
    Task<List<BookRecommendation>> GetRecommendationsAsync(string userId, CancellationToken cancellationToken = default);
}
