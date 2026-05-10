using NetTopologySuite.Geometries;
using Shelter.Domain.Bookings;
using Shelter.Domain.Common;
using Shelter.Domain.Spatial;
using Shelter.Domain.Users;

namespace Shelter.Domain.Shelters;

public class Shelter
{
    private readonly List<ShelterPicture> _pictures = [];

    private Shelter() { }

    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public User? Owner { get; private set; }

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    public int Capacity { get; private set; }
    public ShelterBookingPolicy BookingPolicy { get; private set; }

    public Point Location { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<ShelterPicture> Pictures => _pictures;

    public static Shelter Create(
        Guid ownerId,
        string name,
        string? description,
        int capacity,
        double latitude,
        double longitude,
        ShelterBookingPolicy bookingPolicy,
        DateTimeOffset now)
    {
        ValidateName(name);
        ValidateCapacity(capacity);
        ValidateCoordinates(latitude, longitude);
        ValidateBookingPolicy(bookingPolicy);

        return new Shelter
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = name,
            Description = description,
            Capacity = capacity,
            BookingPolicy = bookingPolicy,
            Location = MakePoint(latitude, longitude),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void UpdateDetails(
        string name,
        string? description,
        int capacity,
        ShelterBookingPolicy bookingPolicy,
        DateTimeOffset now)
    {
        ValidateName(name);
        ValidateCapacity(capacity);
        ValidateBookingPolicy(bookingPolicy);

        Name = name;
        Description = description;
        Capacity = capacity;
        BookingPolicy = bookingPolicy;
        UpdatedAt = now;
    }

    public void Relocate(double latitude, double longitude, DateTimeOffset now)
    {
        ValidateCoordinates(latitude, longitude);

        Location = MakePoint(latitude, longitude);
        UpdatedAt = now;
    }

    public ShelterPicture AddPicture(Guid assetId, string? caption, DateTimeOffset now)
    {
        if (assetId == Guid.Empty)
            throw new DomainValidationException("Picture asset id must be provided.");

        var picture = new ShelterPicture(Guid.NewGuid(), Id, assetId, caption, _pictures.Count);
        _pictures.Add(picture);
        UpdatedAt = now;
        return picture;
    }

    public void RemovePicture(Guid pictureId, DateTimeOffset now)
    {
        var picture = _pictures.FirstOrDefault(p => p.Id == pictureId);
        if (picture is null) return;

        _pictures.Remove(picture);
        UpdatedAt = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        if (!IsActive) return;
        IsActive = false;
        UpdatedAt = now;
    }

    public void Reactivate(DateTimeOffset now)
    {
        if (IsActive) return;
        IsActive = true;
        UpdatedAt = now;
    }

    public bool OwnedBy(Guid userId) => OwnerId == userId;

    public void AssertCanBeBooked(BookingType type, int guests)
    {
        if (!IsActive)
            throw new DomainValidationException("Shelter is not active.");
        if (!AcceptsBookingType(type))
            throw new DomainValidationException(BookingPolicy switch
            {
                ShelterBookingPolicy.ExclusiveOnly => "Shelter only accepts exclusive bookings.",
                ShelterBookingPolicy.InclusiveOnly => "Shelter only accepts inclusive bookings.",
                _ => "Shelter does not accept this booking type.",
            });
        if (guests > Capacity)
            throw new DomainValidationException(
                $"Guest count ({guests}) exceeds shelter capacity ({Capacity}).");
    }

    /// <summary>
    /// Asserts that <paramref name="candidate"/> can be added to this shelter given the supplied
    /// non-cancelled overlapping bookings. Caller filters overlap by status and time window before
    /// calling — this method does no querying. Throws <see cref="DomainValidationException"/> on
    /// conflict; returns silently if the booking fits.
    /// </summary>
    public void AssertCanFit(IReadOnlyList<BookingPeriod> overlapping, BookingPeriod candidate)
    {
        AssertCanBeBooked(candidate.Type, candidate.Guests);

        if (overlapping.Count == 0) return;

        if (candidate.Type == BookingType.Exclusive)
            throw new DomainValidationException(
                "Cannot create exclusive booking — shelter has existing bookings during this period.");

        if (overlapping.Any(b => b.Type == BookingType.Exclusive))
            throw new DomainValidationException(
                "Cannot create booking — shelter is exclusively booked during this period.");

        var peak = PeakInclusiveGuests(overlapping, candidate.StartUtc, candidate.EndUtc);
        if (peak + candidate.Guests > Capacity)
            throw new DomainValidationException(
                $"Insufficient capacity — requested {candidate.Guests} guests but only " +
                $"{Capacity - peak} spots available during this period.");
    }

    /// <summary>
    /// Peak concurrent inclusive guests within <c>[windowStartUtc, windowEndUtc)</c> across the
    /// supplied bookings. Exclusive bookings are filtered out — callers handle exclusive-overlap
    /// rejection separately.
    /// </summary>
    /// <remarks>
    /// Sweepline: peak can only occur at a candidate moment in
    /// {windowStartUtc} ∪ {b.StartUtc for b in overlapping bookings, clamped to the window}.
    /// Booking moments outside any booking interval contribute zero, so they're skipped.
    /// O(n²) — fine for the partial-availability filter on a single shelter's overlapping set.
    /// </remarks>
    public static int PeakInclusiveGuests(
        IReadOnlyList<BookingPeriod> overlapping,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc)
    {
        var inclusive = overlapping.Where(b => b.Type == BookingType.Inclusive).ToList();
        if (inclusive.Count == 0) return 0;

        var candidates = new List<DateTimeOffset> { windowStartUtc };
        candidates.AddRange(inclusive
            .Select(b => b.StartUtc)
            .Where(t => t > windowStartUtc && t < windowEndUtc));

        return candidates.Max(t => inclusive
            .Where(b => b.StartUtc <= t && t < b.EndUtc)
            .Sum(b => b.Guests));
    }

    private bool AcceptsBookingType(BookingType type) => BookingPolicy switch
    {
        ShelterBookingPolicy.ExclusiveOnly => type == BookingType.Exclusive,
        ShelterBookingPolicy.InclusiveOnly => type == BookingType.Inclusive,
        ShelterBookingPolicy.Both => true,
        _ => false,
    };

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Shelter name must be provided.");
    }

    private static void ValidateCapacity(int capacity)
    {
        if (capacity <= 0)
            throw new DomainValidationException("Capacity must be positive.");
    }

    private static void ValidateCoordinates(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
            throw new DomainValidationException("Latitude must be in [-90, 90].");
        if (longitude is < -180 or > 180)
            throw new DomainValidationException("Longitude must be in [-180, 180].");
    }

    private static Point MakePoint(double latitude, double longitude) =>
        new(longitude, latitude) { SRID = SpatialReference.Wgs84 };

    private static void ValidateBookingPolicy(ShelterBookingPolicy bookingPolicy)
    {
        if (!Enum.IsDefined(bookingPolicy))
            throw new DomainValidationException("Unknown booking policy.");
    }
}
