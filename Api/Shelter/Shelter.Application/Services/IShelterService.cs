using Shelter.Application.Requests;
using Shelter.Application.Responses;

namespace Shelter.Application.Services;

public interface IShelterService
{
    Task<ShelterDetailResponse> CreateAsync(CreateShelterRequest request, Guid ownerId, List<FileUpload>? pictures = null);
    Task<ShelterDetailResponse?> GetByIdAsync(Guid id);
    Task<List<ShelterSearchResponse>> SearchAsync(ShelterSearchCriteria searchCriteria);
    Task<List<BookingResponse>> GetBookingsAsync(
        Guid shelterId,
        DateTimeOffset? from,
        DateTimeOffset? to);
    Task<(List<ReviewResponse> Reviews, int TotalCount)> GetReviewsAsync(
        Guid shelterId,
        int page = 1,
        int pageSize = 10);
    Task<bool> UpdateAsync(Guid id, UpdateShelterRequest request);
    Task<bool> DeleteAsync(Guid id);
}

