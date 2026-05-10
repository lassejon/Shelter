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
    BookingApprovalMode BookingApprovalMode,
    bool IsActive,
    List<PictureResponse> Pictures,
    ReviewSummary ReviewSummary)
{
    private const int MaxPicturesInSearch = 2;

    public static SearchShelterResponse FromDomain(
        ShelterEntity shelter,
        IFileStorage storage,
        ReviewSummary reviewSummary) => new(
        shelter.Id,
        shelter.Name,
        shelter.Description,
        shelter.Capacity,
        shelter.Location.Y,
        shelter.Location.X,
        shelter.BookingPolicy,
        shelter.BookingApprovalMode,
        shelter.IsActive,
        shelter.Pictures
            .OrderBy(p => p.SortOrder)
            .Take(MaxPicturesInSearch)
            .Select(p => new PictureResponse(p.Id, storage.GetPublicUrl(p.Asset.BlobKey), p.Caption, p.SortOrder))
            .ToList(),
        reviewSummary);
}
