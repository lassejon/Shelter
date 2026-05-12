using App.Common.Email;

namespace Shelter.Infrastructure.Email;

/// <summary>
/// Dev fallback that writes the rendered email to <see cref="ILogger"/> instead of dispatching
/// it. Selected automatically when SendGrid:ApiKey is empty so a fresh checkout still works
/// without API credentials. Production must use <see cref="SendGridEmailSender"/>.
/// </summary>
internal sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(
        string toAddress,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            """
            === DEV EMAIL (no SendGrid key configured) ===
            To:      {To}
            Subject: {Subject}

            {Text}
            ===============================================
            """,
            toAddress, subject, textBody);
        return Task.CompletedTask;
    }
}
