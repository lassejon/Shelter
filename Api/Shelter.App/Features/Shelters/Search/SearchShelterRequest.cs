namespace App.Features.Shelters.Search;

public class SearchShelterRequest
{
    public double? MinLatitude { get; set; }
    public double? MaxLatitude { get; set; }
    public double? MinLongitude { get; set; }
    public double? MaxLongitude { get; set; }
    public int? Limit { get; set; }
    public int? MinRating { get; set; }
    public int? MinCapacity { get; set; }
    public int? MaxCapacity { get; set; }

    /// <summary>
    /// Start of the desired booking window (inclusive). When both StartUtc and EndUtc are set, shelters with any
    /// overlapping non-cancelled booking are excluded from the result set.
    /// </summary>
    public DateTimeOffset? StartUtc { get; set; }

    /// <summary>
    /// End of the desired booking window (exclusive). See <see cref="StartUtc"/>.
    /// </summary>
    public DateTimeOffset? EndUtc { get; set; }
}
