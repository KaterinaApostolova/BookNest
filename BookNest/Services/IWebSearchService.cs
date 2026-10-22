namespace BookNest.Services;

public interface IWebSearchService
{
    Task<string?> SearchAsync(string query, CancellationToken cancellationToken = default);
}