using Shelter.Domain.Bookings;
using Shelter.Domain.Common;
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

    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

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
            Latitude = latitude,
            Longitude = longitude,
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

        Latitude = latitude;
        Longitude = longitude;
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

    private static void ValidateBookingPolicy(ShelterBookingPolicy bookingPolicy)
    {
        if (!Enum.IsDefined(bookingPolicy))
            throw new DomainValidationException("Unknown booking policy.");
    }
}
