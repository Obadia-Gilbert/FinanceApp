namespace FinanceApp.Infrastructure.Email;

/// <summary>
/// Turns an <see cref="EmailTemplate"/> into the HTML + plaintext bodies
/// that ultimately hit Brevo / SMTP. The implementation owns the entire
/// brand layout — call sites only describe content.
/// </summary>
public interface IEmailTemplateRenderer
{
    /// <summary>Render the template as an email-client-safe HTML document.</summary>
    string RenderHtml(EmailTemplate template);

    /// <summary>Render a plaintext fallback (heading, paragraphs, CTAs, footer).</summary>
    string RenderText(EmailTemplate template);
}
