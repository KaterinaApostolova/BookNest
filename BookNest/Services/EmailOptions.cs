namespace BookNest.Services;

public sealed class EmailOptions
{
    public BrevoOptions Brevo { get; set; } = new();
}

public sealed class BrevoOptions
{
    /// <summary>Free API key from https://app.brevo.com/settings/keys/api (300 free emails/day, no card).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Must be a sender address verified in your Brevo account.</summary>
    public string SenderEmail { get; set; } = string.Empty;

    public string SenderName { get; set; } = "BookNest";
}
