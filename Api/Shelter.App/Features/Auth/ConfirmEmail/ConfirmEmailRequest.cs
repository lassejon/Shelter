namespace App.Features.Auth.ConfirmEmail;

public sealed class ConfirmEmailRequest
{
    public Guid UserId { get; init; }
    public string Token { get; init; } = string.Empty;
}
