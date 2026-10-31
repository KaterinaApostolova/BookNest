namespace BookNest.Services;

public sealed class LyraOptions
{
    public GroqOptions Groq { get; set; } = new();
    public TavilyOptions Tavily { get; set; } = new();
}

public sealed class GroqOptions
{
    /// <summary>
    /// Free API key from https://console.groq.com/keys. Never commit a real value here —
    /// set it via User Secrets locally and an App Service setting in production.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Free-tier Groq chat model. "openai/gpt-oss-120b" is Groq's recommended high-reasoning
    /// replacement after llama-3.3-70b-versatile was decommissioned (Aug 2026).
    /// </summary>
    public string Model { get; set; } = "openai/gpt-oss-120b";
}

public sealed class TavilyOptions
{
    /// <summary>
    /// Free API key from https://tavily.com (1,000 free searches/month, no card required).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
