using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace FinanceApp.Infrastructure.Email;

/// <summary>
/// Renders an <see cref="EmailTemplate"/> into the branded HTML wrapper used
/// by every FinanceApp transactional email, plus a clean plaintext fallback.
/// </summary>
/// <remarks>
/// Layout rules:
/// <list type="bullet">
///   <item>Single 600px max-width card, table-based layout, all inline styles.</item>
///   <item>Header band with logo + thin brand-color rule.</item>
///   <item>Hidden preheader span (1px / display:none) immediately after &lt;body&gt;
///   so the inbox preview text is controllable.</item>
///   <item>CTAs use the bulletproof button pattern (Outlook VML + table fallback).</item>
///   <item>Footer with copyright, optional unsubscribe, support contact, address.</item>
///   <item><c>html lang</c> set from <see cref="CultureInfo.CurrentUICulture"/>.</item>
/// </list>
/// </remarks>
public sealed class EmailTemplateRenderer : IEmailTemplateRenderer
{
    // Neutrals — kept inline so we never need an external stylesheet.
    private const string TextColor = "#1f2937";
    private const string MutedColor = "#6b7280";
    private const string BackgroundColor = "#f3f4f6";
    private const string CardColor = "#ffffff";
    private const string BorderColor = "#e5e7eb";
    private const string FontStack =
        "-apple-system, BlinkMacSystemFont, \"Segoe UI\", Roboto, \"Helvetica Neue\", Arial, sans-serif";

    private readonly EmailBrandingOptions _options;

    public EmailTemplateRenderer(IOptions<EmailBrandingOptions> options)
    {
        _options = options.Value;
    }

    // ----- HTML -----

    public string RenderHtml(EmailTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        var lang = WebUtility.HtmlEncode(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        var brand = WebUtility.HtmlEncode(_options.BrandName);
        var primary = SafeColor(_options.PrimaryColor, "#0d6efd");
        var logoUrl = WebUtility.HtmlEncode(_options.ResolveLogoUrl());
        var preheader = WebUtility.HtmlEncode(template.PreheaderText ?? string.Empty);
        var subject = WebUtility.HtmlEncode(template.Subject ?? string.Empty);
        var heading = WebUtility.HtmlEncode(template.Heading ?? string.Empty);

        var sb = new StringBuilder(4096);
        sb.Append("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" ")
          .Append("\"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">")
          .Append("<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:v=\"urn:schemas-microsoft-com:vml\" ")
          .Append("xmlns:o=\"urn:schemas-microsoft-com:office:office\" lang=\"").Append(lang).Append("\">")
          .Append("<head>")
          .Append("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />")
          .Append("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\" />")
          .Append("<meta name=\"color-scheme\" content=\"light dark\" />")
          .Append("<meta name=\"supported-color-schemes\" content=\"light dark\" />")
          .Append("<title>").Append(subject).Append("</title>")
          .Append("<!--[if mso]><style type=\"text/css\">table,td,div,h1,h2,h3,h4,p{font-family:Arial,Helvetica,sans-serif !important;}</style><![endif]-->")
          .Append("</head>")
          .Append("<body style=\"margin:0;padding:0;background-color:").Append(BackgroundColor)
          .Append(";color:").Append(TextColor).Append(";font-family:").Append(FontStack).Append(";\">");

        // Hidden preheader — controls the inbox preview text.
        sb.Append("<div style=\"display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;")
          .Append("opacity:0;overflow:hidden;mso-hide:all;color:").Append(BackgroundColor).Append(";\">")
          .Append(preheader).Append("</div>");

        // Outer 100% width table for background coverage.
        sb.Append("<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" width=\"100%\" ")
          .Append("style=\"background-color:").Append(BackgroundColor).Append(";\"><tr><td align=\"center\" style=\"padding:24px 12px;\">");

        // Inner 600px max-width card.
        sb.Append("<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" width=\"600\" ")
          .Append("style=\"max-width:600px;width:100%;background-color:").Append(CardColor)
          .Append(";border-radius:12px;border:1px solid ").Append(BorderColor)
          .Append(";box-shadow:0 1px 2px rgba(15,23,42,0.04);overflow:hidden;\">");

        // Header: logo + thin brand rule.
        sb.Append("<tr><td style=\"padding:24px 32px 20px;\">")
          .Append("<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" width=\"100%\"><tr>")
          .Append("<td align=\"left\" style=\"font-family:").Append(FontStack)
          .Append(";font-size:16px;font-weight:600;color:").Append(TextColor).Append(";\">")
          .Append("<img src=\"").Append(logoUrl).Append("\" alt=\"").Append(brand)
          .Append("\" height=\"36\" style=\"display:block;height:36px;width:auto;border:0;outline:none;text-decoration:none;\" />")
          .Append("</td></tr></table></td></tr>");

        sb.Append("<tr><td style=\"padding:0 32px;\">")
          .Append("<div style=\"height:3px;background-color:").Append(primary).Append(";border-radius:2px;\"></div>")
          .Append("</td></tr>");

        // Main content.
        sb.Append("<tr><td style=\"padding:24px 32px 12px;font-family:").Append(FontStack)
          .Append(";font-size:16px;line-height:1.55;color:").Append(TextColor).Append(";\">")
          .Append("<h1 style=\"margin:0 0 16px;font-family:").Append(FontStack)
          .Append(";font-size:22px;line-height:1.3;font-weight:700;color:").Append(TextColor).Append(";\">")
          .Append(heading).Append("</h1>");

        foreach (var block in template.Body)
            RenderBlockHtml(sb, block, primary);

        sb.Append("</td></tr>");

        // Footer.
        sb.Append("<tr><td style=\"padding:8px 32px 28px;\">")
          .Append("<div style=\"border-top:1px solid ").Append(BorderColor).Append(";margin-top:16px;\"></div>")
          .Append("<div style=\"padding-top:16px;font-family:").Append(FontStack)
          .Append(";font-size:12px;line-height:1.5;color:").Append(MutedColor).Append(";\">");

        if (!string.IsNullOrWhiteSpace(template.FooterNote))
        {
            sb.Append("<div style=\"margin-bottom:8px;\">")
              .Append(WebUtility.HtmlEncode(template.FooterNote!)).Append("</div>");
        }

        sb.Append("<div>&copy; ").Append(DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture))
          .Append(' ').Append(brand).Append("</div>");

        if (!string.IsNullOrWhiteSpace(_options.CompanyAddress))
        {
            sb.Append("<div style=\"margin-top:4px;\">")
              .Append(WebUtility.HtmlEncode(_options.CompanyAddress)).Append("</div>");
        }

        if (!string.IsNullOrWhiteSpace(_options.SupportEmail))
        {
            sb.Append("<div style=\"margin-top:4px;\">")
              .Append(WebUtility.HtmlEncode(_options.SupportEmail)).Append("</div>");
        }

        var unsub = template.UnsubscribeUrl ?? _options.UnsubscribeUrl;
        if (!string.IsNullOrWhiteSpace(unsub))
        {
            sb.Append("<div style=\"margin-top:8px;\"><a href=\"")
              .Append(WebUtility.HtmlEncode(unsub))
              .Append("\" style=\"color:").Append(MutedColor)
              .Append(";text-decoration:underline;\">Unsubscribe</a></div>");
        }

        sb.Append("</div></td></tr>");

        sb.Append("</table></td></tr></table></body></html>");

        return sb.ToString();
    }

    private static void RenderBlockHtml(StringBuilder sb, EmailBlock block, string primary)
    {
        switch (block)
        {
            case EmailBlock.Paragraph p:
                sb.Append("<p style=\"margin:0 0 14px;font-family:").Append(FontStack)
                  .Append(";font-size:16px;line-height:1.55;color:").Append(TextColor).Append(";\">")
                  .Append(p.Html).Append("</p>");
                break;

            case EmailBlock.Heading h:
                {
                    var level = Math.Clamp(h.Level, 2, 4);
                    var size = level switch { 2 => "20px", 3 => "17px", _ => "15px" };
                    sb.Append("<h").Append(level)
                      .Append(" style=\"margin:18px 0 10px;font-family:").Append(FontStack)
                      .Append(";font-size:").Append(size).Append(";line-height:1.3;font-weight:600;color:")
                      .Append(TextColor).Append(";\">").Append(WebUtility.HtmlEncode(h.Text))
                      .Append("</h").Append(level).Append(">");
                    break;
                }

            case EmailBlock.CallToAction cta:
                RenderButtonHtml(sb, cta.Label, cta.Url, primary);
                break;

            case EmailBlock.InfoBox box:
                RenderInfoBoxHtml(sb, box);
                break;

            case EmailBlock.KeyValueList list:
                RenderKeyValueListHtml(sb, list);
                break;
        }
    }

    private static void RenderButtonHtml(StringBuilder sb, string label, string url, string primary)
    {
        var safeUrl = WebUtility.HtmlEncode(url ?? string.Empty);
        var safeLabel = WebUtility.HtmlEncode(label ?? string.Empty);

        sb.Append("<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" ")
          .Append("style=\"margin:18px 0 22px;\"><tr><td>")
          // Outlook VML fallback so the rounded button renders on Windows mail clients.
          .Append("<!--[if mso]>")
          .Append("<v:roundrect xmlns:v=\"urn:schemas-microsoft-com:vml\" xmlns:w=\"urn:schemas-microsoft-com:office:word\" ")
          .Append("href=\"").Append(safeUrl).Append("\" style=\"height:44px;v-text-anchor:middle;width:240px;\" arcsize=\"14%\" stroke=\"f\" fillcolor=\"")
          .Append(primary).Append("\"><w:anchorlock/>")
          .Append("<center style=\"color:#ffffff;font-family:Arial,Helvetica,sans-serif;font-size:15px;font-weight:600;\">")
          .Append(safeLabel).Append("</center></v:roundrect>")
          .Append("<![endif]-->")
          .Append("<!--[if !mso]><!-- -->")
          .Append("<a href=\"").Append(safeUrl)
          .Append("\" style=\"background-color:").Append(primary)
          .Append(";border-radius:8px;color:#ffffff;display:inline-block;font-family:").Append(FontStack)
          .Append(";font-size:15px;font-weight:600;line-height:44px;text-align:center;text-decoration:none;")
          .Append("padding:0 26px;mso-padding-alt:0;\">")
          .Append(safeLabel).Append("</a>")
          .Append("<!--<![endif]-->")
          .Append("</td></tr></table>");
    }

    private static void RenderInfoBoxHtml(StringBuilder sb, EmailBlock.InfoBox box)
    {
        var (bg, border, fg) = box.Tone switch
        {
            InfoBoxTone.Success => ("#ecfdf5", "#10b981", "#065f46"),
            InfoBoxTone.Warning => ("#fffbeb", "#f59e0b", "#92400e"),
            InfoBoxTone.Danger => ("#fef2f2", "#ef4444", "#991b1b"),
            _ => ("#eff6ff", "#3b82f6", "#1e3a8a"),
        };

        sb.Append("<div style=\"margin:14px 0;padding:14px 16px;background-color:").Append(bg)
          .Append(";border-left:4px solid ").Append(border)
          .Append(";border-radius:6px;font-family:").Append(FontStack)
          .Append(";font-size:15px;line-height:1.5;color:").Append(fg).Append(";\">")
          .Append(box.Html).Append("</div>");
    }

    private static void RenderKeyValueListHtml(StringBuilder sb, EmailBlock.KeyValueList list)
    {
        sb.Append("<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" width=\"100%\" ")
          .Append("style=\"margin:12px 0 18px;border:1px solid ").Append(BorderColor)
          .Append(";border-radius:8px;border-collapse:separate;\">");

        for (var i = 0; i < list.Items.Count; i++)
        {
            var item = list.Items[i];
            var isLast = i == list.Items.Count - 1;
            sb.Append("<tr><td style=\"padding:10px 14px;font-family:").Append(FontStack)
              .Append(";font-size:13px;color:").Append(MutedColor)
              .Append(";width:40%;border-bottom:").Append(isLast ? "none" : "1px solid " + BorderColor)
              .Append(";\">").Append(WebUtility.HtmlEncode(item.Label))
              .Append("</td><td style=\"padding:10px 14px;font-family:").Append(FontStack)
              .Append(";font-size:14px;font-weight:600;color:").Append(TextColor)
              .Append(";border-bottom:").Append(isLast ? "none" : "1px solid " + BorderColor)
              .Append(";\">").Append(WebUtility.HtmlEncode(item.Value)).Append("</td></tr>");
        }

        sb.Append("</table>");
    }

    // ----- Plaintext -----

    public string RenderText(EmailTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        var sb = new StringBuilder(1024);
        sb.AppendLine(template.Subject ?? string.Empty);
        sb.AppendLine(new string('=', Math.Min((template.Subject ?? string.Empty).Length, 60)));
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(template.Heading))
        {
            sb.AppendLine(template.Heading);
            sb.AppendLine();
        }

        foreach (var block in template.Body)
            RenderBlockText(sb, block);

        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(template.FooterNote))
        {
            sb.AppendLine(template.FooterNote);
            sb.AppendLine();
        }

        sb.Append("— ").AppendLine(_options.BrandName);
        sb.Append('©').Append(' ').Append(DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture))
          .Append(' ').AppendLine(_options.BrandName);

        if (!string.IsNullOrWhiteSpace(_options.SupportEmail))
            sb.AppendLine(_options.SupportEmail);

        var unsub = template.UnsubscribeUrl ?? _options.UnsubscribeUrl;
        if (!string.IsNullOrWhiteSpace(unsub))
            sb.Append("Unsubscribe: ").AppendLine(unsub);

        return sb.ToString();
    }

    private static void RenderBlockText(StringBuilder sb, EmailBlock block)
    {
        switch (block)
        {
            case EmailBlock.Paragraph p:
                sb.AppendLine(StripHtml(p.Html));
                sb.AppendLine();
                break;
            case EmailBlock.Heading h:
                sb.AppendLine(h.Text);
                sb.AppendLine();
                break;
            case EmailBlock.CallToAction cta:
                sb.Append(cta.Label).Append(": ").AppendLine(cta.Url);
                sb.AppendLine();
                break;
            case EmailBlock.InfoBox box:
                sb.AppendLine(StripHtml(box.Html));
                sb.AppendLine();
                break;
            case EmailBlock.KeyValueList list:
                foreach (var item in list.Items)
                    sb.Append("- ").Append(item.Label).Append(": ").AppendLine(item.Value);
                sb.AppendLine();
                break;
        }
    }

    private static readonly Regex TagStripper = new("<[^>]+>", RegexOptions.Compiled);
    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        var stripped = TagStripper.Replace(html, " ");
        return WebUtility.HtmlDecode(Regex.Replace(stripped, "\\s+", " ").Trim());
    }

    private static readonly Regex HexColor = new("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", RegexOptions.Compiled);
    private static string SafeColor(string? value, string fallback)
        => !string.IsNullOrWhiteSpace(value) && HexColor.IsMatch(value!.Trim()) ? value!.Trim() : fallback;
}
