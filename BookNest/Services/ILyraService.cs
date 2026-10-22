namespace BookNest.Services;

public interface ILyraService
{
    Task<string> AskAsync(string message, int? bookId = null, IReadOnlyList<LyraTurn>? history = null, string? userId = null, CancellationToken cancellationToken = default);
}

public sealed record LyraTurn(bool IsUser, string Content);