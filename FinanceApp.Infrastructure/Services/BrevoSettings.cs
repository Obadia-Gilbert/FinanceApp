namespace FinanceApp.Infrastructure.Services;

/// <summary>
/// Configuration for Brevo (ex-Sendinblue) transactional email sending via the
/// HTTP API at <c>https://api.brevo.com/v3/smtp/email</c>.
/// </summary>
/// <remarks>
/// When <see cref="ApiKey"/> is present the API takes priority over
/// <see cref="EmailSettings"/>; otherwise the SMTP <see cref="EmailService"/>
/// is used (which can itself point at Brevo's SMTP relay).
/// </remarks>
public class BrevoSettings
{
    public string ApiKey { get; set; } = string.Empty;

    public string SenderName { get; set; } = string.Empty;

    public string SenderEmail { get; set; } = string.Empty;

    /// <summary>Optional reply-to address (defaults to the sender).</summary>
    public string? ReplyToEmail { get; set; }

    /// <summary>Optional reply-to display name.</summary>
    public string? ReplyToName { get; set; }

    /// <summary>HTTP timeout in milliseconds. Defaults to 10s.</summary>
    public int TimeoutMs { get; set; } = 10000;
}
