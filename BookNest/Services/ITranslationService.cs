namespace BookNest.Services;

public interface ITranslationService
{
    Task<string?> TranslateAsync(string text, string targetLanguage, CancellationToken cancellationToken = default);
}
