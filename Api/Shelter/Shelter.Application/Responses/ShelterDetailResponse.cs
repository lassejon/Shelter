using Shelter.Domain.Shelters;
using ShelterModel = Shelter.Domain.Shelters.Shelter;

namespace Shelter.Application.Responses;

public record ShelterDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    int Capacity,
    double Latitude,
    double Longitude,
    ShelterBookingPolicy BookingPolicy,
    bool IsActive,
    Guid OwnerId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    List<PictureResponse> Pictures,
    ReviewSummary ReviewSummary)
{
    public static ShelterDetailResponse FromDomain(ShelterModel shelter) => new(
        shelter.Id,
        shelter.Name,
        shelter.Description,
        shelter.Capacity,
        shelter.Location.Y, // Latitude
        shelter.Location.X, // Longitude
        shelter.BookingPolicy,
        shelter.IsActive,
        shelter.OwnerId,
        shelter.CreatedAt,
        shelter.UpdatedAt,
        shelter.Pictures
            .OrderBy(p => p.SortOrder)
            .Take(10)  // Max 10 pictures
            .Select(PictureResponse.FromDomain)
            .ToList(),
        ReviewSummary.FromReviews(shelter.Reviews));
}
