using App.Common.Email;
using Microsoft.Extensions.DependencyInjection;
using Shelter.Infrastructure.Email;
using Shelter.Infrastructure.Settings.Base;

namespace Shelter.Infrastructure.Settings;

/// <summary>
/// SendGrid configuration. Bound from the <c>SendGrid</c> config section, populated in dev via
/// <c>dotnet user-secrets</c>. When <see cref="ApiKey"/> is empty the
/// <see cref="LoggingEmailSender"/> is registered instead, so local dev works without a key.
/// </summary>
internal sealed class SendGridSettings : Settings<SendGridSettings>
{
    public string ApiKey { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = "Shelter";

    public override IServiceCollection OnConfigure(IServiceCollection services)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        }
        else
        {
            services.AddSingleton(this);
            services.AddSingleton<IEmailSender, SendGridEmailSender>();
        }
        return services;
    }
}
