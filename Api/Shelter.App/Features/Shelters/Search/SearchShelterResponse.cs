using App.Features.Shelters.Shared;
using Shelter.Domain.Shelters;
using ShelterEntity = Shelter.Domain.Shelters.Shelter;

namespace App.Features.Shelters.Search;

public record SearchShelterResponse(
    Guid Id,
    string Name,
    string? Description,
    int Capacity,
    double Latitude,
    double Longitude,
    ShelterBookingPolicy BookingPolicy,
    bool IsActive,
    List<ShelterPictureResponse> Pictures,
    ShelterReviewSummary ReviewSummary)
{
    private const int MaxPicturesInSearch = 2;

    public static SearchShelterResponse FromDomain(ShelterEntity shelter) => new(
        shelter.Id,
        shelter.Name,
        shelter.Description,
        shelter.Capacity,
        shelter.Latitude,
        shelter.Longitude,
        shelter.BookingPolicy,
        shelter.IsActive,
        shelter.Pictures
            .OrderBy(p => p.SortOrder)
            .Take(MaxPicturesInSearch)
            .Select(p => new ShelterPictureResponse(p.Id, p.Url, p.Caption, p.SortOrder))
            .ToList(),
        ShelterReviewSummary.Empty);
}

public record ShelterReviewSummary(double AverageRating, int TotalCount)
{
    public static ShelterReviewSummary Empty { get; } = new(0d, 0);
}
