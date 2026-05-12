using App.Common;
using App.Common.Email;
using Microsoft.AspNetCore.Identity;
using Shelter.Domain.Auth;
using Shelter.Domain.Users;

namespace App.Features.Auth.Register;

public enum RegisterFailure
{
    EmailAlreadyExists,
    InvalidPassword,
}

public sealed class RegisterHandler(
    UserManager<User> userManager,
    IEmailSender emailSender,
    FrontendUrls frontendUrls,
    ILogger<RegisterHandler> logger)
{
    public async Task<(RegisterResponse? response, RegisterFailure? failure, IReadOnlyList<string>? errors)> HandleAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            logger.LogInformation("Register failed: email already exists ({Email})", request.Email);
            return (null, RegisterFailure.EmailAlreadyExists, null);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
        };

        var create = await userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
        {
            var errors = create.Errors.Select(e => e.Description).ToList();
            logger.LogInformation("Register failed for {Email}: {Errors}", request.Email, string.Join("; ", errors));
            return (null, RegisterFailure.InvalidPassword, errors);
        }

        if (request.IsShelterOwner)
        {
            await userManager.AddToRoleAsync(user, AppRoles.ShelterOwner);
        }

        await SendConfirmationEmailAsync(user, cancellationToken);

        logger.LogInformation("Registered user {UserId}; awaiting email confirmation", user.Id);

        return (new RegisterResponse(user.Email!, RequiresEmailConfirmation: true), null, null);
    }

    private async Task SendConfirmationEmailAsync(User user, CancellationToken cancellationToken)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmUrl = BuildConfirmUrl(user.Id, token);

        var subject = "Confirm your Shelter account";
        var textBody =
            $"Welcome to Shelter!\n\n" +
            $"Please confirm your email by clicking the link below:\n\n" +
            $"{confirmUrl}\n\n" +
            $"If you didn't create this account, you can safely ignore this email.";
        var htmlBody =
            $"""
            <p>Welcome to Shelter!</p>
            <p>Please confirm your email by clicking the button below:</p>
            <p><a href="{confirmUrl}" style="display:inline-block;padding:12px 20px;background:#1f8a6e;color:#fff;text-decoration:none;border-radius:6px;">Confirm email</a></p>
            <p>Or paste this link into your browser:<br><code>{confirmUrl}</code></p>
            <p style="color:#64748b;font-size:13px;">If you didn't create this account, you can safely ignore this email.</p>
            """;

        await emailSender.SendAsync(user.Email!, subject, htmlBody, textBody, cancellationToken);
    }

    private string BuildConfirmUrl(Guid userId, string token) =>
        $"{frontendUrls.BaseUrl.TrimEnd('/')}/auth/confirm-email" +
        $"?userId={userId}" +
        $"&token={Uri.EscapeDataString(token)}";
}
