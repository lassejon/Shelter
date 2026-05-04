using App.Common;
using App.Features.Reviews.Shared;
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
    List<PictureResponse> Pictures,
    ReviewSummary ReviewSummary)
{
    private const int MaxPicturesInSearch = 2;

    public static SearchShelterResponse FromDomain(ShelterEntity shelter, IFileStorage storage) => new(
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
            .Select(p => new PictureResponse(p.Id, storage.GetPublicUrl(p.Asset.BlobKey), p.Caption, p.SortOrder))
            .ToList(),
        // TODO: project per-shelter ReviewSummary in SearchShelterHandler in search response.
        ReviewSummary.Empty);
}
