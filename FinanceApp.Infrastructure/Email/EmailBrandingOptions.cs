namespace FinanceApp.Infrastructure.Email;

/// <summary>
/// Brand tokens consumed by the email renderer. Single source of truth for
/// logo URL, primary color, sender identity, and CAN-SPAM/GDPR footer copy.
/// Bound from the <c>EmailBranding</c> section of <c>appsettings.json</c> in
/// both FinanceApp.API and FinanceApp.Web.
/// </summary>
public class EmailBrandingOptions
{
    public const string SectionName = "EmailBranding";

    /// <summary>
    /// Public base URL of the Web app (e.g. <c>https://app.financeapp.io</c>).
    /// Used to build absolute URLs for the logo and other assets when
    /// <see cref="LogoUrl"/> is left empty.
    /// </summary>
    public string WebAppBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Absolute URL of the email logo. Defaults to
    /// <c>{WebAppBaseUrl}/branding/email-logo.png</c> when empty.
    /// </summary>
    public string LogoUrl { get; set; } = string.Empty;

    /// <summary>Display name shown in the header band and footer.</summary>
    public string BrandName { get; set; } = "FinanceApp";

    /// <summary>Primary brand color (hex), used for header rule + CTA buttons.</summary>
    public string PrimaryColor { get; set; } = "#0d6efd";

    /// <summary>Support email shown in the footer "questions?" line.</summary>
    public string SupportEmail { get; set; } = "support@financeapp.io";

    /// <summary>Physical mailing address for CAN-SPAM/GDPR compliance.</summary>
    public string CompanyAddress { get; set; } = string.Empty;

    /// <summary>Optional unsubscribe URL shown in the footer.</summary>
    public string UnsubscribeUrl { get; set; } = string.Empty;

    /// <summary>
    /// Feature flag: send a welcome email after successful registration.
    /// Defaults to <c>true</c> — set to <c>false</c> in config to disable.
    /// </summary>
    public bool SendWelcomeEmail { get; set; } = true;

    /// <summary>Returns the configured logo URL, falling back to the
    /// <c>{WebAppBaseUrl}/branding/email-logo.png</c> convention.</summary>
    public string ResolveLogoUrl()
    {
        if (!string.IsNullOrWhiteSpace(LogoUrl))
            return LogoUrl.Trim();

        var baseUrl = (WebAppBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        return string.IsNullOrEmpty(baseUrl)
            ? "/branding/email-logo.png"
            : baseUrl + "/branding/email-logo.png";
    }
}
