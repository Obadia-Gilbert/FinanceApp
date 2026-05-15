using System;
using System.Collections.Generic;
using System.Globalization;
using FinanceApp.Infrastructure.Email;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinanceApp.API.Tests.Unit;

/// <summary>
/// Snapshot-style assertions on the branded HTML/text wrapper produced by
/// <see cref="EmailTemplateRenderer"/>. We avoid pinning the exact markup —
/// it can evolve — and instead verify the load-bearing pieces every email
/// must have: logo URL, brand color, hidden preheader, heading, bulletproof
/// CTA, copyright, and plaintext flattening.
/// </summary>
public class EmailTemplateRendererTests
{
    private static EmailTemplateRenderer CreateRenderer(EmailBrandingOptions? options = null) =>
        new(Options.Create(options ?? new EmailBrandingOptions
        {
            WebAppBaseUrl = "https://app.financeapp.test",
            BrandName = "FinanceApp",
            PrimaryColor = "#0d6efd",
            SupportEmail = "support@financeapp.test",
            CompanyAddress = "FinanceApp Ltd, P.O. Box 1, City",
        }));

    private static EmailTemplate SampleResetTemplate(string url = "https://app.financeapp.test/reset?code=abc&email=user%40example.com") =>
        new(
            Subject: "Reset your FinanceApp password",
            PreheaderText: "Use the link below to set a new password — expires in 2 hours.",
            Heading: "Hi Alex, let's reset your password",
            Body: new EmailBlock[]
            {
                new EmailBlock.Paragraph("We received a request to reset the password for your FinanceApp account."),
                new EmailBlock.CallToAction("Reset my password", url),
                new EmailBlock.Paragraph("This link expires in 2 hours."),
            },
            FooterNote: "You're receiving this because you have a FinanceApp account.");

    [Fact]
    public void RenderHtml_IncludesBrandPalette_LogoAndPreheader()
    {
        var renderer = CreateRenderer();

        var html = renderer.RenderHtml(SampleResetTemplate());

        Assert.Contains("https://app.financeapp.test/branding/email-logo.png", html);
        Assert.Contains("alt=\"FinanceApp\"", html);
        Assert.Contains("#0d6efd", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Use the link below to set a new password", html);
        Assert.Contains("display:none", html);
        Assert.Contains("Hi Alex, let&#39;s reset your password", html);
        Assert.Contains("&copy; " + DateTime.UtcNow.Year, html);
        Assert.Contains("FinanceApp Ltd, P.O. Box 1, City", html);
    }

    [Fact]
    public void RenderHtml_CtaUsesBulletproofPattern_VmlAndAnchorFallback()
    {
        var renderer = CreateRenderer();
        var template = SampleResetTemplate("https://example.com/r?code=xyz");

        var html = renderer.RenderHtml(template);

        Assert.Contains("<v:roundrect", html);
        Assert.Contains("href=\"https://example.com/r?code=xyz\"", html);
        Assert.Contains("<!--[if mso]>", html);
        Assert.Contains("<!--[if !mso]>", html);
        Assert.Contains(">Reset my password<", html);
    }

    [Fact]
    public void RenderHtml_LangAttribute_FollowsCurrentUiCulture()
    {
        var renderer = CreateRenderer();
        var template = SampleResetTemplate();

        WithCulture("es", () =>
        {
            var html = renderer.RenderHtml(template);
            Assert.Contains("lang=\"es\"", html);
        });

        WithCulture("sw", () =>
        {
            var html = renderer.RenderHtml(template);
            Assert.Contains("lang=\"sw\"", html);
        });
    }

    [Fact]
    public void RenderHtml_ExplicitLogoUrl_OverridesBaseUrlConvention()
    {
        var renderer = CreateRenderer(new EmailBrandingOptions
        {
            WebAppBaseUrl = "https://app.financeapp.test",
            LogoUrl = "https://cdn.financeapp.test/email-logo.png",
            BrandName = "FinanceApp",
            PrimaryColor = "#0d6efd"
        });

        var html = renderer.RenderHtml(SampleResetTemplate());

        Assert.Contains("src=\"https://cdn.financeapp.test/email-logo.png\"", html);
        Assert.DoesNotContain("https://app.financeapp.test/branding/email-logo.png", html);
    }

    [Fact]
    public void RenderHtml_RejectsBogusPrimaryColor_FallsBackToDefault()
    {
        var renderer = CreateRenderer(new EmailBrandingOptions
        {
            WebAppBaseUrl = "https://app.financeapp.test",
            BrandName = "FinanceApp",
            PrimaryColor = "javascript:alert(1)"
        });

        var html = renderer.RenderHtml(SampleResetTemplate());

        Assert.DoesNotContain("javascript", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#0d6efd", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderHtml_InfoBox_AppliesToneColors()
    {
        var renderer = CreateRenderer();
        var template = new EmailTemplate(
            Subject: "Budget alert",
            PreheaderText: "Over budget",
            Heading: "Hi Alex",
            Body: new EmailBlock[]
            {
                new EmailBlock.InfoBox("You went over your monthly limit.", InfoBoxTone.Danger)
            });

        var html = renderer.RenderHtml(template);

        // Danger tone uses red border (#ef4444) per renderer table.
        Assert.Contains("#ef4444", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("You went over your monthly limit.", html);
    }

    [Fact]
    public void RenderHtml_KeyValueList_RendersAllRows()
    {
        var renderer = CreateRenderer();
        var template = new EmailTemplate(
            Subject: "Budget alert",
            PreheaderText: "Over budget",
            Heading: "Budget detail",
            Body: new EmailBlock[]
            {
                new EmailBlock.KeyValueList(new List<KeyValueItem>
                {
                    new("Category", "Groceries"),
                    new("Spent", "520,000.00 TZS"),
                    new("Limit", "500,000.00 TZS"),
                })
            });

        var html = renderer.RenderHtml(template);

        Assert.Contains("Category", html);
        Assert.Contains("Groceries", html);
        Assert.Contains("520,000.00 TZS", html);
        Assert.Contains("500,000.00 TZS", html);
    }

    [Fact]
    public void RenderText_FlattensCtaAsLabelColonUrl_AndKeepsHeading()
    {
        var renderer = CreateRenderer();

        var text = renderer.RenderText(SampleResetTemplate("https://example.com/reset"));

        Assert.Contains("Reset your FinanceApp password", text);
        Assert.Contains("Hi Alex, let's reset your password", text);
        Assert.Contains("Reset my password: https://example.com/reset", text);
        Assert.Contains("© " + DateTime.UtcNow.Year + " FinanceApp", text);
        // No HTML tags should leak through to plaintext.
        Assert.DoesNotContain("<p", text);
        Assert.DoesNotContain("<a ", text);
    }

    [Fact]
    public void RenderHtml_PreheaderText_IsHiddenFromBodyButPresent()
    {
        var renderer = CreateRenderer();
        var template = new EmailTemplate(
            Subject: "Welcome",
            PreheaderText: "Your FinanceApp account is ready.",
            Heading: "Welcome",
            Body: new EmailBlock[] { new EmailBlock.Paragraph("Hello") });

        var html = renderer.RenderHtml(template);

        var idx = html.IndexOf("Your FinanceApp account is ready.", StringComparison.Ordinal);
        Assert.True(idx > 0, "Preheader text must appear in the HTML.");
        // Ensure the preheader sits inside the hidden div (display:none) — i.e.
        // somewhere between the body open and the visible content table.
        var hiddenIdx = html.IndexOf("display:none", StringComparison.Ordinal);
        Assert.True(hiddenIdx > 0 && hiddenIdx < idx,
            "Preheader must appear after a display:none container so clients hide it.");
    }

    private static void WithCulture(string code, Action action)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            var culture = new CultureInfo(code);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }
}
