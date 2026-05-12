using App.Common;
using App.Common.Email;
using Microsoft.AspNetCore.Identity;
using Shelter.Domain.Users;

namespace App.Features.Auth.ResendConfirmationEmail;

/// <summary>
/// Re-sends the email confirmation link. Always succeeds (returns 204) regardless of whether
/// the email exists or is already confirmed — the absence of a leak about account existence
/// is the security property the public endpoint trades for.
/// </summary>
public sealed class ResendConfirmationEmailHandler(
    UserManager<User> userManager,
    IEmailSender emailSender,
    FrontendUrls frontendUrls,
    ILogger<ResendConfirmationEmailHandler> logger)
{
    public async Task HandleAsync(
        ResendConfirmationEmailRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            logger.LogInformation("Resend confirmation: no user with email {Email}", request.Email);
            return;
        }

        if (user.EmailConfirmed)
        {
            logger.LogInformation("Resend confirmation: {UserId} already confirmed", user.Id);
            return;
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmUrl =
            $"{frontendUrls.BaseUrl.TrimEnd('/')}/auth/confirm-email" +
            $"?userId={user.Id}" +
            $"&token={Uri.EscapeDataString(token)}";

        const string subject = "Your new Shelter confirmation link";
        var textBody =
            $"Here's a fresh link to confirm your Shelter account:\n\n" +
            $"{confirmUrl}\n\n" +
            $"If you didn't request this, you can ignore the email.";
        var htmlBody =
            $"""
            <p>Here's a fresh link to confirm your Shelter account:</p>
            <p><a href="{confirmUrl}" style="display:inline-block;padding:12px 20px;background:#1f8a6e;color:#fff;text-decoration:none;border-radius:6px;">Confirm email</a></p>
            <p>Or paste this link into your browser:<br><code>{confirmUrl}</code></p>
            <p style="color:#64748b;font-size:13px;">If you didn't request this, you can ignore the email.</p>
            """;

        await emailSender.SendAsync(user.Email!, subject, htmlBody, textBody, cancellationToken);

        logger.LogInformation("Resend confirmation email queued for {UserId}", user.Id);
    }
}
