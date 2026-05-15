using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using FinanceApp.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace FinanceApp.Infrastructure.Email;

/// <summary>
/// One factory method per notification kind. Pulls every string from
/// <see cref="IStringLocalizer{SharedResource}"/> so subjects, headings, and
/// body copy automatically honour the current request UI culture (en / es / sw).
/// </summary>
/// <remarks>
/// To add a new notification kind:
/// <list type="number">
///   <item>Add <c>Email_&lt;Kind&gt;_*</c> keys to
///   <c>FinanceApp.Localization/SharedResource.resx</c> (and the .es / .sw siblings).</item>
///   <item>Add a <c>BuildX</c> factory here returning an <see cref="EmailTemplate"/>.</item>
///   <item>Call it from the relevant controller / page model.</item>
///   <item>Add a <c>BuildX</c> test in <c>LocalizedEmailTemplatesTests</c>.</item>
/// </list>
/// </remarks>
public sealed class LocalizedEmailTemplates
{
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly EmailBrandingOptions _options;

    public LocalizedEmailTemplates(
        IStringLocalizer<SharedResource> localizer,
        IOptions<EmailBrandingOptions> options)
    {
        _localizer = localizer;
        _options = options.Value;
    }

    // ---------------- Reset password ----------------

    public EmailTemplate BuildResetPassword(string userName, string resetUrl)
    {
        var safeName = SafeName(userName);
        return new EmailTemplate(
            Subject: Get("Email_ResetPassword_Subject"),
            PreheaderText: Get("Email_ResetPassword_Preheader"),
            Heading: Format("Email_ResetPassword_Heading", safeName),
            Body: new EmailBlock[]
            {
                new EmailBlock.Paragraph(WebUtility.HtmlEncode(Get("Email_ResetPassword_BodyIntro"))),
                new EmailBlock.CallToAction(
                    Label: Get("Email_ResetPassword_CtaLabel"),
                    Url: resetUrl ?? string.Empty),
                new EmailBlock.Paragraph(WebUtility.HtmlEncode(Get("Email_ResetPassword_BodyExpiry"))),
                new EmailBlock.Paragraph(WebUtility.HtmlEncode(Get("Email_ResetPassword_BodyIgnore"))),
            },
            FooterNote: Get("Email_Common_Footer"),
            UnsubscribeUrl: null);
    }

    // ---------------- Welcome ----------------

    public EmailTemplate BuildWelcome(string userName, string appUrl)
    {
        var safeName = SafeName(userName);
        var url = string.IsNullOrWhiteSpace(appUrl) ? _options.WebAppBaseUrl : appUrl;
        var blocks = new List<EmailBlock>
        {
            new EmailBlock.Paragraph(WebUtility.HtmlEncode(Get("Email_Welcome_BodyIntro"))),
            new EmailBlock.Paragraph(WebUtility.HtmlEncode(Get("Email_Welcome_BodyTips"))),
        };
        if (!string.IsNullOrWhiteSpace(url))
            blocks.Add(new EmailBlock.CallToAction(Get("Email_Welcome_CtaLabel"), url));

        return new EmailTemplate(
            Subject: Format("Email_Welcome_Subject", _options.BrandName),
            PreheaderText: Get("Email_Welcome_Preheader"),
            Heading: Format("Email_Welcome_Heading", safeName),
            Body: blocks,
            FooterNote: Get("Email_Common_Footer"));
    }

    // ---------------- Email confirmation ----------------

    public EmailTemplate BuildEmailConfirmation(string userName, string confirmationUrl)
    {
        var safeName = SafeName(userName);
        return new EmailTemplate(
            Subject: Get("Email_Confirm_Subject"),
            PreheaderText: Get("Email_Confirm_Preheader"),
            Heading: Format("Email_Confirm_Heading", safeName),
            Body: new EmailBlock[]
            {
                new EmailBlock.Paragraph(WebUtility.HtmlEncode(Get("Email_Confirm_BodyIntro"))),
                new EmailBlock.CallToAction(Get("Email_Confirm_CtaLabel"), confirmationUrl ?? string.Empty),
                new EmailBlock.Paragraph(WebUtility.HtmlEncode(Get("Email_Confirm_BodyIgnore"))),
            },
            FooterNote: Get("Email_Common_Footer"));
    }

    // ---------------- Budget alert ----------------

    public EmailTemplate BuildBudgetAlert(
        string userName,
        string categoryName,
        decimal spent,
        decimal limit,
        string currency)
    {
        var safeName = SafeName(userName);
        var spentStr = FormatMoney(spent, currency);
        var limitStr = FormatMoney(limit, currency);
        var pct = limit > 0
            ? Math.Round(spent / limit * 100m, 0, MidpointRounding.AwayFromZero)
            : 100m;
        var tone = spent >= limit ? InfoBoxTone.Danger : InfoBoxTone.Warning;
        var infoKey = spent >= limit ? "Email_BudgetAlert_BodyOver" : "Email_BudgetAlert_BodyNear";

        return new EmailTemplate(
            Subject: Format("Email_BudgetAlert_Subject", categoryName),
            PreheaderText: Format("Email_BudgetAlert_Preheader", categoryName),
            Heading: Format("Email_BudgetAlert_Heading", safeName),
            Body: new EmailBlock[]
            {
                new EmailBlock.Paragraph(string.Format(
                    CultureInfo.CurrentUICulture,
                    Get(infoKey),
                    WebUtility.HtmlEncode(categoryName),
                    spentStr,
                    limitStr,
                    pct.ToString("0", CultureInfo.CurrentUICulture))),
                new EmailBlock.KeyValueList(new List<KeyValueItem>
                {
                    new(Get("Email_BudgetAlert_LabelCategory"), categoryName),
                    new(Get("Email_BudgetAlert_LabelSpent"), spentStr),
                    new(Get("Email_BudgetAlert_LabelLimit"), limitStr),
                    new(Get("Email_BudgetAlert_LabelPercent"),
                        pct.ToString("0", CultureInfo.CurrentUICulture) + "%"),
                }),
                new EmailBlock.InfoBox(
                    WebUtility.HtmlEncode(Get("Email_BudgetAlert_BodyAdvice")), tone),
            },
            FooterNote: Get("Email_Common_Footer"));
    }

    // ---------------- Daily activity reminder ----------------

    public EmailTemplate BuildDailyReminder(string userName, string appUrl)
    {
        var safeName = SafeName(userName);
        var url = string.IsNullOrWhiteSpace(appUrl) ? _options.WebAppBaseUrl : appUrl;
        var blocks = new List<EmailBlock>
        {
            new EmailBlock.Paragraph(WebUtility.HtmlEncode(Get("Email_DailyReminder_BodyIntro"))),
        };
        if (!string.IsNullOrWhiteSpace(url))
            blocks.Add(new EmailBlock.CallToAction(Get("Email_DailyReminder_CtaLabel"), url));
        blocks.Add(new EmailBlock.Paragraph(WebUtility.HtmlEncode(Get("Email_DailyReminder_BodyOptOut"))));

        return new EmailTemplate(
            Subject: Get("Email_DailyReminder_Subject"),
            PreheaderText: Get("Email_DailyReminder_Preheader"),
            Heading: Format("Email_DailyReminder_Heading", safeName),
            Body: blocks,
            FooterNote: Get("Email_Common_Footer"));
    }

    // ---------------- Feedback acknowledgement ----------------

    public EmailTemplate BuildFeedbackAcknowledgement(string userName)
    {
        var safeName = SafeName(userName);
        return new EmailTemplate(
            Subject: Get("Email_FeedbackAck_Subject"),
            PreheaderText: Get("Email_FeedbackAck_Preheader"),
            Heading: Format("Email_FeedbackAck_Heading", safeName),
            Body: new EmailBlock[]
            {
                new EmailBlock.Paragraph(WebUtility.HtmlEncode(Get("Email_FeedbackAck_BodyIntro"))),
                new EmailBlock.Paragraph(WebUtility.HtmlEncode(Get("Email_FeedbackAck_BodyClose"))),
            },
            FooterNote: Get("Email_Common_Footer"));
    }

    // ---------------- Generic fallback ----------------

    public EmailTemplate BuildGeneric(string subject, string heading, params string[] bodyParagraphs)
    {
        var blocks = new List<EmailBlock>();
        foreach (var p in bodyParagraphs ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(p))
                blocks.Add(new EmailBlock.Paragraph(WebUtility.HtmlEncode(p)));
        }

        return new EmailTemplate(
            Subject: subject ?? string.Empty,
            PreheaderText: heading ?? string.Empty,
            Heading: heading ?? string.Empty,
            Body: blocks,
            FooterNote: Get("Email_Common_Footer"));
    }

    // ---------------- Helpers ----------------

    private string Get(string key)
    {
        var value = _localizer[key];
        // IStringLocalizer returns ResourceNotFound text equal to the key when missing;
        // fall back gracefully so a missing translation does not crash a send.
        return value.ResourceNotFound ? key : value.Value;
    }

    private string Format(string key, params object[] args)
    {
        var template = Get(key);
        try
        {
            return string.Format(CultureInfo.CurrentUICulture, template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    private static string FormatMoney(decimal amount, string currency)
    {
        var number = amount.ToString("N2", CultureInfo.CurrentUICulture);
        return string.IsNullOrWhiteSpace(currency) ? number : number + " " + currency;
    }

    private string SafeName(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName)) return Get("Email_Common_DefaultName");
        var trimmed = userName.Trim();
        var space = trimmed.IndexOf(' ');
        var first = space > 0 ? trimmed.Substring(0, space) : trimmed;
        return first;
    }
}
