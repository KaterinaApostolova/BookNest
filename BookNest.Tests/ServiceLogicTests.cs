using System.Globalization;
using BookNest.Services;

namespace BookNest.Tests;

public class ServiceLogicTests
{
    [SetUp]
    public void SetUp()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
    }

    [TearDown]
    public void TearDown()
    {
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    [Test]
    public void GenreTranslator_ReturnsEnglishName_WhenCultureIsEnglish()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("en");

        var result = GenreTranslator.Localize("Fantasy");

        Assert.That(result, Is.EqualTo("Fantasy"));
    }

    [Test]
    public void GenreTranslator_ReturnsBulgarianName_WhenCultureIsBulgarian_AndGenreIsKnown()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("bg");

        var result = GenreTranslator.Localize("Fantasy");

        Assert.That(result, Is.EqualTo("Фентъзи"));
    }

    [Test]
    public void GenreTranslator_FallsBackToOriginalName_WhenGenreIsUnknown()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("bg");

        var result = GenreTranslator.Localize("Steampunk Cyberpunk Mashup");

        Assert.That(result, Is.EqualTo("Steampunk Cyberpunk Mashup"));
    }

    [Test]
    public void GenreTranslator_IsCaseInsensitive()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("bg");

        var result = GenreTranslator.Localize("fantasy");

        Assert.That(result, Is.EqualTo("Фентъзи"));
    }

    [Test]
    public void QuoteService_ReturnsNonEmptyQuote_ForEachCollection()
    {
        var service = new QuoteService();

        foreach (QuoteCollection collection in Enum.GetValues(typeof(QuoteCollection)))
        {
            var quote = service.GetQuote(collection);
            Assert.That(string.IsNullOrWhiteSpace(quote), Is.False, $"Expected a quote for {collection}");
        }
    }

    [Test]
    public void QuoteService_ReturnsBulgarianQuote_WhenCultureIsBulgarian()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("bg");
        var service = new QuoteService();

        var quote = service.GetQuote(QuoteCollection.Register);

        // Bulgarian quotes use Cyrillic characters; a plain ASCII-only match would fail.
        Assert.That(quote.Any(c => c >= 'А' && c <= 'я'), Is.True);
    }

    [Test]
    public void ShouldSearchWeb_ReturnsTrue_WhenNoBookContextExists()
    {
        var result = LyraContextBuilder.ShouldSearchWeb("Tell me about this random topic", bookContext: null);

        Assert.That(result, Is.True);
    }

    [Test]
    public void ShouldSearchWeb_ReturnsTrue_WhenBookDescriptionIsMissing()
    {
        var bookContext = "Source: BookNest\nTitle: Some Book\nDescription: Not available";

        var result = LyraContextBuilder.ShouldSearchWeb("What is the plot?", bookContext);

        Assert.That(result, Is.True);
    }

    [Test]
    public void ShouldSearchWeb_ReturnsFalse_WhenBookContextIsCompleteAndQueryIsUnrelatedToCurrentEvents()
    {
        var bookContext = "Source: BookNest\nTitle: Some Book\nDescription: A full description here.";

        var result = LyraContextBuilder.ShouldSearchWeb("What is the average rating?", bookContext);

        Assert.That(result, Is.False);
    }

    [Test]
    public void ShouldSearchWeb_ReturnsTrue_WhenQueryAsksAboutPlotEvenWithBookContext()
    {
        var bookContext = "Source: BookNest\nTitle: Some Book\nDescription: A full description here.";

        var result = LyraContextBuilder.ShouldSearchWeb("Can you summarize the plot?", bookContext);

        Assert.That(result, Is.True);
    }

    [Test]
    public void BuildPrompt_IncludesConversationHistory_WhenProvided()
    {
        var history = new List<LyraTurn>
        {
            new(IsUser: true, Content: "Hello"),
            new(IsUser: false, Content: "Hi there!")
        };

        var prompt = LyraContextBuilder.BuildPrompt("What's next?", bookContext: null, webContext: null, history);

        Assert.That(prompt, Does.Contain("Hello"));
        Assert.That(prompt, Does.Contain("Hi there!"));
        Assert.That(prompt, Does.Contain("What's next?"));
    }
}
