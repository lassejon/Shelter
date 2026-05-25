using System.Text.Json.Serialization;
using NetTopologySuite.IO.Converters;

namespace Shelter.Api.Configuration;

public static class JsonConfigurationExtensions
{
    /// <summary>
    /// Configures JSON serialization for ASP.NET Core Minimal APIs.
    /// </summary>
    public static IServiceCollection ConfigureJsonSerialization(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            // Prevent circular reference issues when serializing related entities.
            // Example: Shelter has a Pictures collection; each ShelterPicture has a Shelter
            // Without IgnoreCycles: { "id": "s1", "pictures": [ { "id": "p1", "shelter": { "id": "s1", "pictures": [ { "id": "p1", "shelter": { ... } } ] } } ] }
            // With IgnoreCycles: { "id": "s1", "pictures": [ { "id": "p1", "shelter": null } ] }
            options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

            // Serialize NetTopologySuite geometry types as GeoJSON (Point.X/Y as doubles, etc.)
            options.SerializerOptions.Converters.Add(new GeoJsonConverterFactory());
        });

        return services;
    }
}
