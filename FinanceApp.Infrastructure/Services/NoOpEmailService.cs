using System.Threading.Tasks;
using FinanceApp.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Infrastructure.Services;

/// <summary>
/// Email sender that swallows messages instead of sending them. Used when SMTP
/// is not configured (e.g. local API runs without EmailSettings, integration
/// tests) so password-reset and other flows can complete without throwing.
/// </summary>
public class NoOpEmailService : IEmailService
{
    private readonly ILogger<NoOpEmailService>? _logger;

    public NoOpEmailService(ILogger<NoOpEmailService>? logger = null)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body)
    {
        _logger?.LogWarning(
            "Email not sent — EmailSettings:SmtpServer is not configured. " +
            "Would have sent to '{To}' with subject '{Subject}'.",
            to, subject);
        return Task.CompletedTask;
    }
}
