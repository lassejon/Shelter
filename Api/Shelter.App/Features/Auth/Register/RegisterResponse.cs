namespace App.Features.Auth.Register;

/// <summary>
/// Response shape after a successful registration. Unlike the prior auto-login flow, the
/// account is now <em>not</em> activated until the user clicks the confirmation link sent to
/// the supplied email; <see cref="RequiresEmailConfirmation"/> is always <c>true</c> in the
/// current MVP and is included so the UI can branch deterministically.
/// </summary>
public sealed record RegisterResponse(string Email, bool RequiresEmailConfirmation);
