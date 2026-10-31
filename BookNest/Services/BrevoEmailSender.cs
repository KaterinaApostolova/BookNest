using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace BookNest.Services;

public class BrevoEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly EmailOptions _options;
    private readonly ILogger<BrevoEmailSender> _logger;

    public BrevoEmailSender(HttpClient httpClient, IOptions<EmailOptions> options, ILogger<BrevoEmailSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Brevo.ApiKey) || string.IsNullOrWhiteSpace(_options.Brevo.SenderEmail))
        {
            _logger.LogWarning("Email:Brevo:ApiKey or SenderEmail is not configured.");
            return false;
        }

        try
        {
            var request = new BrevoEmailRequest
            {
                Sender = new BrevoContact { Email = _options.Brevo.SenderEmail, Name = _options.Brevo.SenderName },
                To = new List<BrevoContact> { new() { Email = toEmail } },
                Subject = subject,
                HtmlContent = htmlBody
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v3/smtp/email")
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Add("api-key", _options.Brevo.ApiKey);
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Brevo returned HTTP status {StatusCode}: {Body}", response.StatusCode, body);
                return false;
            }

            return true;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "Failed to send email via Brevo.");
            return false;
        }
    }

    private sealed class BrevoEmailRequest
    {
        [JsonPropertyName("sender")]
        public BrevoContact Sender { get; init; } = new();

        [JsonPropertyName("to")]
        public List<BrevoContact> To { get; init; } = new();

        [JsonPropertyName("subject")]
        public string Subject { get; init; } = string.Empty;

        [JsonPropertyName("htmlContent")]
        public string HtmlContent { get; init; } = string.Empty;
    }

    private sealed class BrevoContact
    {
        [JsonPropertyName("email")]
        public string Email { get; init; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }
}
