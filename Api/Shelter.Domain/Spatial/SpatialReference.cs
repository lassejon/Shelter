namespace Shelter.Domain.Spatial;

/// <summary>
/// Spatial reference identifiers used across the system. Centralised so the literal
/// SRID never drifts between the entity factory, EF column type, search query, and tests.
/// </summary>
public static class SpatialReference
{
    /// <summary>
    /// EPSG:4326 — WGS 84 geographic coordinates (lat/long on the WGS 84 spheroid).
    /// The single SRID we store in <c>Shelter.Location</c> and use for every spatial query.
    /// </summary>
    public const int Wgs84 = 4326;
}
