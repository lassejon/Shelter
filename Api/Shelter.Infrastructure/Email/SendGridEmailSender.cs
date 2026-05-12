using App.Common.Email;
using SendGrid;
using SendGrid.Helpers.Mail;
using Shelter.Infrastructure.Settings;

namespace Shelter.Infrastructure.Email;

/// <summary>
/// Production email sender backed by SendGrid's transactional API. Registered when
/// <see cref="SendGridSettings.ApiKey"/> is non-empty. Logs (without leaking the body) on
/// non-success status so deliverability issues surface in the API logs.
/// </summary>
internal sealed class SendGridEmailSender(
    SendGridSettings settings,
    ILogger<SendGridEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(
        string toAddress,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken)
    {
        var client = new SendGridClient(settings.ApiKey);
        var from = new EmailAddress(settings.FromEmail, settings.FromName);
        var to = new EmailAddress(toAddress);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, textBody, htmlBody);

        var response = await client.SendEmailAsync(msg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var status = response.StatusCode;
            var body = await response.Body.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "SendGrid send failed for {To}: {Status} {Body}",
                toAddress, status, body);
            throw new InvalidOperationException(
                $"SendGrid returned {status}. Check the API logs for the response body.");
        }
    }
}
