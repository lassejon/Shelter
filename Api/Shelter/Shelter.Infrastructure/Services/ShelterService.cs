using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Shelter.Application.Interfaces;
using Shelter.Application.Requests;
using Shelter.Application.Responses;
using Shelter.Application.Services;
using Shelter.Domain.Pictures;
using Shelter.Infrastructure.Persistence;
using NetTopologySuite.Geometries;

namespace Shelter.Infrastructure.Services;

public class ShelterService(
    ShelterDbContext context,
    IFileStorage fileStorage,
    ILogger<ShelterService> logger) : IShelterService
{
    private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);

    public async Task<ShelterDetailResponse> CreateAsync(CreateShelterRequest request, Guid ownerId, List<FileUpload>? pictures = null)
    {
        var shelter = request.ToDomain(ownerId);

        context.Shelters.Add(shelter);
        await context.SaveChangesAsync();

        // Upload pictures if provided
        if (pictures != null && pictures.Count > 0)
        {
            for (int i = 0; i < pictures.Count; i++)
            {
                var file = pictures[i];

                // Upload to storage
                var url = await fileStorage.SaveShelterPictureAsync(
                    shelter.Id,
                    file.Content,
                    file.ContentType);

                // Create picture entity
                var picture = new Picture
                {
                    Id = Guid.NewGuid(),
                    OwnerId = ownerId,
                    Scope = PictureScope.Shelter,
                    ShelterId = shelter.Id,
                    Url = url,
                    Caption = null,
                    SortOrder = i,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                context.Pictures.Add(picture);
            }

            await context.SaveChangesAsync();

            // Reload shelter with pictures to get fresh data
            shelter = await context.Shelters
                .Include(s => s.Pictures)
                .Include(s => s.Reviews)
                .FirstAsync(s => s.Id == shelter.Id);
        }

        logger.LogInformation(
            "Created shelter {ShelterId} with {PictureCount} pictures for owner {OwnerId}",
            shelter.Id,
            shelter.Pictures.Count,
            ownerId);

        return ShelterDetailResponse.FromDomain(shelter);
    }

    public async Task<ShelterDetailResponse?> GetByIdAsync(Guid id)
    {
        var shelter = await context.Shelters
            .Include(s => s.Pictures)
            .Include(s => s.Reviews)
            .FirstOrDefaultAsync(s => s.Id == id);

        return shelter == null ? null : ShelterDetailResponse.FromDomain(shelter);
    }

    public async Task<List<ShelterSearchResponse>> SearchAsync(ShelterSearchCriteria searchCriteria)
    {
        var query = context.Shelters
            .Include(s => s.Pictures)
            .Include(s => s.Reviews)
            .Where(s => true);

        if (searchCriteria is { MinLatitude: not null, MaxLatitude: not null, MinLongitude: not null, MaxLongitude: not null })
        {
            var envelope = new Envelope(searchCriteria.MinLongitude.Value, searchCriteria.MaxLongitude.Value, searchCriteria.MinLatitude.Value, searchCriteria.MaxLatitude.Value);
            var bounds = GeometryFactory.ToGeometry(envelope);
            query = query.Where(s => s.Location.Within(bounds));
        }

        query = query
            .OrderBy(s => s.Name);

        if (searchCriteria.Limit.HasValue)
        {
            query = query.Take(searchCriteria.Limit.Value);
        }
        
        var shelters = await query.ToListAsync();

        return shelters.Select(ShelterSearchResponse.FromDomain).ToList();
    }

    public async Task<List<BookingResponse>> GetBookingsAsync(
        Guid shelterId,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        var query = context.Bookings
            .Include(b => b.Booker)
            .Where(b => b.ShelterId == shelterId);

        if (from.HasValue)
        {
            // Booking overlaps if: booking ends after 'from'
            query = query.Where(b => b.EndUtc >= from.Value);
        }

        if (to.HasValue)
        {
            // Booking overlaps if: booking starts before 'to'
            query = query.Where(b => b.StartUtc <= to.Value);
        }

        var bookings = await query
            .OrderBy(b => b.StartUtc)
            .ToListAsync();

        return bookings.Select(BookingResponse.FromDomain).ToList();
    }

    public async Task<(List<ReviewResponse> Reviews, int TotalCount)> GetReviewsAsync(
        Guid shelterId,
        int page = 1,
        int pageSize = 10)
    {
        var query = context.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.Pictures)
            .Where(r => r.ShelterId == shelterId);

        var totalCount = await query.CountAsync();

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (reviews.Select(ReviewResponse.FromDomain).ToList(), totalCount);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateShelterRequest request)
    {
        var shelter = await context.Shelters.FindAsync(id);

        if (shelter == null)
        {
            return false;
        }

        request.ApplyTo(shelter);

        await context.SaveChangesAsync();

        logger.LogInformation("Updated shelter {ShelterId}", id);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var shelter = await context.Shelters.FindAsync(id);

        if (shelter == null)
        {
            return false;
        }

        context.Shelters.Remove(shelter);
        await context.SaveChangesAsync();

        logger.LogInformation("Deleted shelter {ShelterId}", id);

        return true;
    }
}

