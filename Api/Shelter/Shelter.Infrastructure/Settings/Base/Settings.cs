using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shelter.Infrastructure.Settings.Base;

public abstract class Settings<T> where T : Settings<T>
{
    public abstract IServiceCollection OnConfigure(IServiceCollection services);
}

public static class SettingsExtensions
{
    private const string Suffix = "Settings";

    public static IServiceCollection AddSettings<T>(
        this IServiceCollection services,
        IConfiguration configuration) where T : Settings<T>, new()
    {
        var name = typeof(T).Name;
        if (!name.EndsWith(Suffix))
            throw new InvalidOperationException($"Settings class '{name}' must end with '{Suffix}'.");

        var sectionName = name[..^Suffix.Length];
        var section = configuration.GetSection(sectionName);

        services.Configure<T>(configuration.GetSection(sectionName));
        
        var settings = new T();
        section.Bind(settings);
        
        services = settings.OnConfigure(services);
        return services;
    }
}