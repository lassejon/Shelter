namespace App.Features.Shelters.Search;

public class SearchShelterRequest
{
    // Bbox
    public double? MinLatitude { get; set; }
    public double? MaxLatitude { get; set; }
    public double? MinLongitude { get; set; }
    public double? MaxLongitude { get; set; }
    
    // Filters
    public int? Limit { get; set; }
    public int? MinRating { get; set; }
    public int? MinCapacity { get; set; }
    public int? MaxCapacity { get; set; }

    /// <summary>
    /// Free-text query. When set, shelters are filtered by trigram similarity on
    /// <c>Name</c> (Postgres pg_trgm) and ordered by similarity descending. Tolerates
    /// minor typos. Composes with the bbox / capacity / rating / availability filters.
    /// </summary>
    public string? Q { get; set; }

    /// <summary>
    /// Number of guests for the desired booking window. Combined with StartUtc/EndUtc, shelters are filtered
    /// by *remaining* availability rather than by overlap alone: a partially booked inclusive shelter still
    /// matches as long as Capacity − peakConcurrentBookedGuests ≥ Guests at every moment in the window.
    /// Independently of dates, it raises the effective minimum capacity (Capacity ≥ Guests) so shelters that
    /// can never fit the party are excluded up front.
    /// </summary>
    public int? Guests { get; set; }

    /// <summary>
    /// Start of the desired booking window (inclusive). When both StartUtc and EndUtc are set the handler
    /// filters by date availability — see <see cref="Guests"/> for the semantics.
    /// </summary>
    public DateTimeOffset? StartUtc { get; set; }

    /// <summary>
    /// End of the desired booking window (exclusive). See <see cref="StartUtc"/>.
    /// </summary>
    public DateTimeOffset? EndUtc { get; set; }
}
