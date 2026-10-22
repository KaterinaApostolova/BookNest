namespace BookNest.Interfaces;

public interface IReadingAssistantService
{
    Task<string> GetResponseAsync(string message);
}