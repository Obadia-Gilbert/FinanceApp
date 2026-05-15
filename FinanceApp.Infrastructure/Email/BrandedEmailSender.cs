using System.Threading;
using System.Threading.Tasks;
using FinanceApp.Application.Interfaces;

namespace FinanceApp.Infrastructure.Email;

/// <inheritdoc cref="IBrandedEmailSender"/>
public sealed class BrandedEmailSender : IBrandedEmailSender
{
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateRenderer _renderer;

    public BrandedEmailSender(IEmailService emailService, IEmailTemplateRenderer renderer)
    {
        _emailService = emailService;
        _renderer = renderer;
    }

    public Task SendAsync(string to, EmailTemplate template, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var html = _renderer.RenderHtml(template);
        // IEmailService.SendEmailAsync does not currently honour CancellationToken; the token
        // here is for forward-compatibility and to satisfy call-site signatures.
        return _emailService.SendEmailAsync(to, template.Subject, html);
    }
}
