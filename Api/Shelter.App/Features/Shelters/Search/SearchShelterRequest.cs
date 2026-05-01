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
}
