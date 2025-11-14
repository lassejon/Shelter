using Microsoft.Extensions.DependencyInjection;

namespace Shelter.Infrastructure.Settings.Base;

public class Settings<T> where T : Settings<T>
{
    private const string Suffix = "Settings";
    
    protected string SectionName { get; private set; } = null!;

    public virtual IServiceCollection OnConfigure(IServiceCollection serviceCollection)
    {
        var sectionName = typeof(T).Name;
        
        if (!sectionName.EndsWith(Suffix))
        {
            throw new InvalidOperationException($"Class name must end with {Suffix}");
        }
        
        SectionName = sectionName;

        return serviceCollection;
    }
}