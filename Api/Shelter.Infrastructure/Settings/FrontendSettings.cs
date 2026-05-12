using App.Common;
using Microsoft.Extensions.DependencyInjection;
using Shelter.Infrastructure.Settings.Base;

namespace Shelter.Infrastructure.Settings;

/// <summary>
/// Frontend deployment metadata that the API needs to build outgoing links (e.g. the email
/// confirmation URL). Bound from the <c>Frontend</c> config section; surfaces an
/// <see cref="App.Common.FrontendUrls"/> singleton so App handlers can inject the contract
/// type without referencing Infrastructure.
/// </summary>
internal sealed class FrontendSettings : Settings<FrontendSettings>
{
    public string BaseUrl { get; init; } = "http://localhost:3000";

    public override IServiceCollection OnConfigure(IServiceCollection services)
    {
        services.AddSingleton(new FrontendUrls(BaseUrl));
        return services;
    }
}
