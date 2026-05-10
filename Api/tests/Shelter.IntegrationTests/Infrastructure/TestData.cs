using App.Persistence;
using Shelter.Domain.Bookings;
using Shelter.Domain.Reviews;
using Shelter.Domain.Shelters;
using Shelter.Domain.Users;
using ShelterEntity = Shelter.Domain.Shelters.Shelter;

namespace Shelter.IntegrationTests.Infrastructure;

/// <summary>
/// Seed helpers for Tier 2 tests. Bypass <see cref="Microsoft.AspNetCore.Identity.UserManager{TUser}"/>
/// — handler tests aren't auth flows; we just need user rows so the FK targets exist.
/// </summary>
internal static class TestData
{
    public static async Task<User> SeedUserAsync(
        IShelterDbContext db,
        string? email = null,
        CancellationToken cancellationToken = default)
    {
        email ??= $"user-{Guid.NewGuid():N}@test.local";
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "User",
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    public static async Task<ShelterEntity> SeedShelterAsync(
        IShelterDbContext db,
        Guid ownerId,
        string name = "Test Shelter",
        string? description = null,
        int capacity = 4,
        double latitude = 56.0,
        double longitude = 10.0,
        ShelterBookingPolicy policy = ShelterBookingPolicy.Both,
        BookingApprovalMode approvalMode = BookingApprovalMode.Instant,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        var shelter = ShelterEntity.Create(
            ownerId, name, description, capacity, latitude, longitude, policy, approvalMode,
            now ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        db.Shelters.Add(shelter);
        await db.SaveChangesAsync(cancellationToken);
        return shelter;
    }

    public static async Task<Booking> SeedBookingAsync(
        IShelterDbContext db,
        Guid shelterId,
        Guid bookerId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        DateTimeOffset today,
        DateTimeOffset now,
        int guests = 2,
        BookingType type = BookingType.Inclusive,
        BookingApprovalMode shelterApprovalMode = BookingApprovalMode.Instant,
        CancellationToken cancellationToken = default)
    {
        var booking = Booking.Create(shelterId, bookerId, startUtc, endUtc, guests, type, shelterApprovalMode, today, now);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken);
        return booking;
    }

    public static async Task<Review> SeedReviewAsync(
        IShelterDbContext db,
        Guid shelterId,
        Guid reviewerId,
        Rating rating = Rating.Good,
        string? comment = null,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        var review = Review.Create(shelterId, reviewerId, rating, comment,
            now ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        db.Reviews.Add(review);
        await db.SaveChangesAsync(cancellationToken);
        return review;
    }
}
