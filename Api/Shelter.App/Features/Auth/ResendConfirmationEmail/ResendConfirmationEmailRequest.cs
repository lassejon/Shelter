namespace App.Features.Auth.ResendConfirmationEmail;

public sealed class ResendConfirmationEmailRequest
{
    public string Email { get; init; } = string.Empty;
}
