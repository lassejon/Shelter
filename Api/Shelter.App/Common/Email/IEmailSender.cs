namespace App.Common.Email;

/// <summary>
/// Sends transactional email out of band. The interface is deliberately minimal — sender
/// implementations decide how the body is rendered (plain text, HTML, templates) and how
/// failures are escalated. Caller awaits delivery acknowledgement (or stub log).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(
        string toAddress,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken);
}
