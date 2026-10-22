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
            "Every journey begins with a single page.",
            "A new chapter starts today.",
            "Stories become even better when they are shared.",
            "Every library begins with a single book. Every reader begins with a single page.",
            "Welcome to the place where stories continue long after the final chapter.",
            "Some books become memories. Some memories become home.",
            "You don't just collect books. You collect pieces of yourself.",
            "Today you're not joining a website. You're joining thousands of stories.",
            "Every reader deserves a place where excitement never has to stay silent.",
            "Every story deserves a place to be remembered.",
            "Every great reader was once someone opening their first book.",
            "Today begins a journey that doesn't end with the last page.",
            "Some stories change us. Others help us find ourselves.",
            "Welcome to a place where every reader belongs.",
            "Every book opens a new door.",
            "“ There is no friend as loyal as a book. ” — Ernest Hemingway",
            "“ A room without books is like a body without a soul. ” — Marcus Tullius Cicero",
            "“ The only thing you absolutely have to know is the location of the library.” — Albert Einstein",
            "“ We read to know we are not alone. ” — C. S. Lewis"
        },
        [QuoteCollection.Login] = new[]
        {
            "Every ending deserves a conversation.",
            "Stories live beyond their pages.",
            "Another adventure begins today.",
            "The last page is only the beginning.",
            "Every reader deserves a place to belong.",
            "Some stories stay with us forever. Let's discover another one.",
            "Every page is easier to turn when someone shares the journey.",
            "Your bookshelf has been waiting for you.",
            "Every return begins with curiosity.",
            "The conversations you missed are waiting between the pages.",
            "Some stories ask to be remembered. Others ask to be shared.",
            "Books patiently wait. So do the people who love them.",
            "Your reading journey never really paused. It simply waited.",
           "“ Not all those who wander are lost. ” — J. R. R. Tolkien",
           "“ It does not do to dwell on dreams and forget to live. ” — J. K. Rowling",
           "“ There is always room for another story. ” — Hans Christian Andersen"
        },
        [QuoteCollection.Logout] = new[]
        {
            "Until your next story.",
            "Books will be waiting.",
            "Every goodbye is another beginning.",
            "The next page is always here.",
            "Every ending is simply another place to begin.",
            "We'll be here when your next story finds you.",
            "Take today's stories with you.",
            "Until the next chapter.",
            "Because no story should end with the last page.",
            "Every goodbye leaves a bookmark for the next chapter.",
            "The best stories are worth returning to.",
            "Take today's emotions with you.",
            "BookNest will always have another page waiting." ,
            "“ Tomorrow is a new day. ” — L. M. Montgomery",
            "“ The world was hers for the reading. ” — Betty Smith",
            "“ There is more treasure in books than in all the pirate's loot. ” — Walt Disney",
            "“ A reader lives a thousand lives before he dies. ” — George R. R. Martin"
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
