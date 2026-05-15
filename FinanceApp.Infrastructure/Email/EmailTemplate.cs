using System.Collections.Generic;

namespace FinanceApp.Infrastructure.Email;

/// <summary>
/// Strongly-typed representation of a branded transactional email. The
/// renderer turns this into HTML + plaintext; templates never build markup
/// themselves, which keeps every email visually consistent.
/// </summary>
public sealed record EmailTemplate(
    string Subject,
    string PreheaderText,
    string Heading,
    IReadOnlyList<EmailBlock> Body,
    string? FooterNote = null,
    string? UnsubscribeUrl = null);

/// <summary>
/// Discriminated union of content blocks that make up an email body.
/// Adding a new block kind requires a new nested record AND a switch arm
/// in <see cref="EmailTemplateRenderer"/>.
/// </summary>
public abstract record EmailBlock
{
    /// <summary>A paragraph. <paramref name="Html"/> is rendered as-is; callers
    /// must HTML-encode any user input.</summary>
    public sealed record Paragraph(string Html) : EmailBlock;

    /// <summary>A heading (h2/h3/h4). <paramref name="Level"/> is clamped to 2..4.</summary>
    public sealed record Heading(int Level, string Text) : EmailBlock;

    /// <summary>A bulletproof CTA button (Outlook VML + table fallback).</summary>
    public sealed record CallToAction(string Label, string Url) : EmailBlock;

    /// <summary>A tinted info / warning box.</summary>
    public sealed record InfoBox(string Html, InfoBoxTone Tone) : EmailBlock;

    /// <summary>A two-column key/value list (e.g. for budget alert details).</summary>
    public sealed record KeyValueList(IReadOnlyList<KeyValueItem> Items) : EmailBlock;
}

/// <summary>One row in an <see cref="EmailBlock.KeyValueList"/>.</summary>
public sealed record KeyValueItem(string Label, string Value);

/// <summary>Visual tone of an <see cref="EmailBlock.InfoBox"/>.</summary>
public enum InfoBoxTone
{
    Info,
    Success,
    Warning,
    Danger
}
