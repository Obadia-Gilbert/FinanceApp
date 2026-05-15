using System.Threading;
using System.Threading.Tasks;

namespace FinanceApp.Infrastructure.Email;

/// <summary>
/// Send-an-<see cref="EmailTemplate"/> facade. Wraps the renderer + the
/// underlying <c>IEmailService</c> so call sites stay one-liner:
/// <code>await _emailSender.SendAsync(user.Email, template, ct);</code>
/// </summary>
public interface IBrandedEmailSender
{
    /// <summary>
    /// Renders <paramref name="template"/> via the registered
    /// <see cref="IEmailTemplateRenderer"/> and dispatches it through the
    /// configured email transport.
    /// </summary>
    Task SendAsync(string to, EmailTemplate template, CancellationToken cancellationToken = default);
}
