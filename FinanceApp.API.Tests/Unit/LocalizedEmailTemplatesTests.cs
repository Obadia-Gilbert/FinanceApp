using System;
using System.Globalization;
using System.Linq;
using FinanceApp.Infrastructure.Email;
using FinanceApp.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinanceApp.API.Tests.Unit;

/// <summary>
/// Verifies <see cref="LocalizedEmailTemplates"/> produces an
/// <see cref="EmailTemplate"/> with the expected blocks, and that subjects /
/// headings shift between en/es/sw cultures.
/// </summary>
public class LocalizedEmailTemplatesTests
{
    private static LocalizedEmailTemplates BuildFactory()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        var provider = services.BuildServiceProvider();
        var localizer = provider.GetRequiredService<IStringLocalizer<SharedResource>>();
        var options = Options.Create(new EmailBrandingOptions
        {
            WebAppBaseUrl = "https://app.financeapp.test",
            BrandName = "FinanceApp",
            PrimaryColor = "#0d6efd",
        });
        return new LocalizedEmailTemplates(localizer, options);
    }

    [Fact]
    public void BuildResetPassword_HasSubjectHeadingAndCtaBlocks()
    {
        var factory = BuildFactory();

        var template = factory.BuildResetPassword("Alex Doe", "https://example.com/reset");

        Assert.False(string.IsNullOrWhiteSpace(template.Subject));
        Assert.Contains("Alex", template.Heading);
        Assert.False(string.IsNullOrWhiteSpace(template.PreheaderText));
        var cta = Assert.Single(template.Body.OfType<EmailBlock.CallToAction>());
        Assert.Equal("https://example.com/reset", cta.Url);
        Assert.False(string.IsNullOrWhiteSpace(cta.Label));
    }

    [Fact]
    public void BuildWelcome_AddsCtaWhenUrlProvided()
    {
        var factory = BuildFactory();

        var template = factory.BuildWelcome("Alex", "https://app.financeapp.test");

        Assert.Single(template.Body.OfType<EmailBlock.CallToAction>());
        Assert.Contains("Alex", template.Heading);
    }

    [Fact]
    public void BuildWelcome_OmitsCta_WhenNoUrlAndNoBaseUrl()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        var provider = services.BuildServiceProvider();
        var localizer = provider.GetRequiredService<IStringLocalizer<SharedResource>>();
        var options = Options.Create(new EmailBrandingOptions
        {
            WebAppBaseUrl = "",
            BrandName = "FinanceApp"
        });
        var factory = new LocalizedEmailTemplates(localizer, options);

        var template = factory.BuildWelcome("Alex", string.Empty);

        Assert.Empty(template.Body.OfType<EmailBlock.CallToAction>());
    }

    [Fact]
    public void BuildEmailConfirmation_AlwaysIncludesCta()
    {
        var factory = BuildFactory();

        var template = factory.BuildEmailConfirmation("Alex", "https://example.com/confirm");

        var cta = Assert.Single(template.Body.OfType<EmailBlock.CallToAction>());
        Assert.Equal("https://example.com/confirm", cta.Url);
    }

    [Fact]
    public void BuildBudgetAlert_OverLimit_UsesDangerInfoBoxAndKeyValueList()
    {
        var factory = BuildFactory();

        var template = factory.BuildBudgetAlert(
            "Alex",
            categoryName: "Groceries",
            spent: 520_000m,
            limit: 500_000m,
            currency: "TZS");

        Assert.Contains("Groceries", template.Subject);
        var box = Assert.Single(template.Body.OfType<EmailBlock.InfoBox>());
        Assert.Equal(InfoBoxTone.Danger, box.Tone);
        var list = Assert.Single(template.Body.OfType<EmailBlock.KeyValueList>());
        Assert.Equal(4, list.Items.Count);
        Assert.Contains(list.Items, i => i.Value.Contains("Groceries"));
    }

    [Fact]
    public void BuildBudgetAlert_NearLimit_UsesWarningInfoBox()
    {
        var factory = BuildFactory();

        var template = factory.BuildBudgetAlert(
            "Alex",
            categoryName: "Groceries",
            spent: 420_000m,
            limit: 500_000m,
            currency: "TZS");

        var box = Assert.Single(template.Body.OfType<EmailBlock.InfoBox>());
        Assert.Equal(InfoBoxTone.Warning, box.Tone);
    }

    [Fact]
    public void BuildDailyReminder_ProducesIntroAndOptOutParagraphs()
    {
        var factory = BuildFactory();

        var template = factory.BuildDailyReminder("Alex", "https://app.financeapp.test");

        Assert.True(template.Body.OfType<EmailBlock.Paragraph>().Count() >= 2);
        Assert.Single(template.Body.OfType<EmailBlock.CallToAction>());
    }

    [Fact]
    public void BuildFeedbackAcknowledgement_HasTwoBodyParagraphs()
    {
        var factory = BuildFactory();

        var template = factory.BuildFeedbackAcknowledgement("Alex");

        Assert.Equal(2, template.Body.OfType<EmailBlock.Paragraph>().Count());
        Assert.Empty(template.Body.OfType<EmailBlock.CallToAction>());
    }

    [Fact]
    public void BuildGeneric_WrapsParagraphsAndSkipsEmptyOnes()
    {
        var factory = BuildFactory();

        var template = factory.BuildGeneric(
            "Subject", "Heading",
            "First paragraph", "", "   ", "Second paragraph");

        var paragraphs = template.Body.OfType<EmailBlock.Paragraph>().ToList();
        Assert.Equal(2, paragraphs.Count);
        Assert.Equal("Subject", template.Subject);
        Assert.Equal("Heading", template.Heading);
    }

    [Theory]
    [InlineData("en", "Reset your FinanceApp password")]
    [InlineData("es", "Restablece tu contraseña de FinanceApp")]
    [InlineData("sw", "Weka upya nenosiri lako la FinanceApp")]
    public void BuildResetPassword_SubjectVariesByCulture(string cultureCode, string expectedSubject)
    {
        var factory = BuildFactory();

        WithCulture(cultureCode, () =>
        {
            var template = factory.BuildResetPassword("Alex", "https://example.com/reset");
            Assert.Equal(expectedSubject, template.Subject);
        });
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
