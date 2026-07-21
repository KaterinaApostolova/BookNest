namespace BookNest.Services;

public enum QuoteCollection
{
    Register,
    Login,
    Logout
}

public class QuoteService
{
    private readonly Random _random = new();

    private static readonly IReadOnlyDictionary<QuoteCollection, string[]> Quotes = new Dictionary<QuoteCollection, string[]>
    {
        [QuoteCollection.Register] = new[]
        {
            "Welcome to BookNest.",
            "Every journey begins with a single page.",
            "A new chapter starts today.",
            "Stories become even better when they are shared.",
            "We are happy you are here."
        },
        [QuoteCollection.Login] = new[]
        {
            "Welcome back.",
            "Every ending deserves a conversation.",
            "Stories live beyond their pages.",
            "Books were waiting for you.",
            "Another adventure begins today.",
            "The last page is only the beginning.",
            "Every reader deserves a place to belong."
        },
        [QuoteCollection.Logout] = new[]
        {
            "See you soon.",
            "Until your next story.",
            "Books will be waiting.",
            "Every goodbye is another beginning.",
            "The next page is always here."
        }
    };

    public string GetQuote(QuoteCollection collection)
    {
        if (!Quotes.TryGetValue(collection, out var values))
        {
            return "Welcome to BookNest.";
        }

        var index = _random.Next(values.Length);
        return values[index];
    }
}
