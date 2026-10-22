using System.Net;
using System.Text.RegularExpressions;

namespace BookNest.Services;

public class FreeWebSearchService : IWebSearchService
{
    private readonly HttpClient _httpClient;

    public FreeWebSearchService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string?> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"html/?q={Uri.EscapeDataString(query)}";
            var html = await _httpClient.GetStringAsync(url, cancellationToken);
            var results = ExtractResults(html).Take(5).ToList();

            return results.Count == 0
                ? null
                : string.Join(Environment.NewLine + Environment.NewLine, results);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    private static IEnumerable<string> ExtractResults(string html)
    {
        var resultPattern = new Regex(
            "<a[^>]*class=\\\"result-link\\\"[^>]*href=\\\"(?<url>[^\\\"]+)\\\"[^>]*>(?<title>.*?)</a>.*?<a[^>]*class=\\\"result-snippet\\\"[^>]*>(?<snippet>.*?)</a>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in resultPattern.Matches(html))
        {
            var title = CleanHtml(match.Groups["title"].Value);
            var snippet = CleanHtml(match.Groups["snippet"].Value);
            var url = WebUtility.HtmlDecode(match.Groups["url"].Value);

            if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(snippet))
            {
                yield return $"Title: {title}\nSnippet: {snippet}\nURL: {url}";
            }
        }
    }

    private static string CleanHtml(string value)
    {
        var withoutTags = Regex.Replace(value, "<.*?>", string.Empty);
        return WebUtility.HtmlDecode(withoutTags).Trim();
    }
}