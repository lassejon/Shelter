using Shelter.Domain.Bookings;
using Shelter.Domain.Common;
using Shelter.Domain.Shelters;
using ShelterEntity = Shelter.Domain.Shelters.Shelter;

namespace Shelter.UnitTests.Domain.Shelters;

public class ShelterTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 7, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid OwnerId = Guid.NewGuid();

    private static ShelterEntity ValidShelter(
        int capacity = 4,
        ShelterBookingPolicy policy = ShelterBookingPolicy.Both) =>
        ShelterEntity.Create(OwnerId, "Birch Hut", "Cozy", capacity, 55.0, 12.0, policy, Now);

    [Fact]
    public void Create_returns_active_shelter_with_invariants_satisfied()
    {
        var shelter = ValidShelter();

        shelter.Id.Should().NotBe(Guid.Empty);
        shelter.OwnerId.Should().Be(OwnerId);
        shelter.IsActive.Should().BeTrue();
        shelter.Pictures.Should().BeEmpty();
        shelter.CreatedAt.Should().Be(Now);
        shelter.UpdatedAt.Should().Be(Now);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_throws_when_name_is_blank(string name)
    {
        var act = () => ShelterEntity.Create(OwnerId, name, null, 4, 0, 0, ShelterBookingPolicy.Both, Now);

        act.Should().Throw<DomainValidationException>().WithMessage("*name*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Create_throws_when_capacity_is_not_positive(int capacity)
    {
        var act = () => ShelterEntity.Create(OwnerId, "Hut", null, capacity, 0, 0, ShelterBookingPolicy.Both, Now);

        act.Should().Throw<DomainValidationException>().WithMessage("*Capacity*");
    }

    [Theory]
    [InlineData(-91, 0)]
    [InlineData(91, 0)]
    [InlineData(0, -181)]
    [InlineData(0, 181)]
    public void Create_throws_when_coordinates_are_out_of_range(double lat, double lng)
    {
        var act = () => ShelterEntity.Create(OwnerId, "Hut", null, 1, lat, lng, ShelterBookingPolicy.Both, Now);

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Create_throws_when_booking_policy_is_undefined()
    {
        var act = () => ShelterEntity.Create(OwnerId, "Hut", null, 1, 0, 0, (ShelterBookingPolicy)99, Now);

        act.Should().Throw<DomainValidationException>().WithMessage("*booking policy*");
    }

    [Fact]
    public void UpdateDetails_revalidates_invariants_and_bumps_timestamp()
    {
        var shelter = ValidShelter();
        var later = Now.AddHours(1);

        shelter.UpdateDetails("New", "desc", 6, ShelterBookingPolicy.InclusiveOnly, later);

        shelter.Name.Should().Be("New");
        shelter.Capacity.Should().Be(6);
        shelter.BookingPolicy.Should().Be(ShelterBookingPolicy.InclusiveOnly);
        shelter.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void UpdateDetails_throws_when_capacity_dropped_to_zero()
    {
        var shelter = ValidShelter();

        var act = () => shelter.UpdateDetails("New", null, 0, ShelterBookingPolicy.Both, Now.AddHours(1));

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Relocate_updates_coordinates_and_bumps_timestamp()
    {
        var shelter = ValidShelter();
        var later = Now.AddHours(2);

        shelter.Relocate(60.0, 10.0, later);

        shelter.Latitude.Should().Be(60.0);
        shelter.Longitude.Should().Be(10.0);
        shelter.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void AddPicture_assigns_sort_order_by_insertion()
    {
        var shelter = ValidShelter();
        var assetA = Guid.NewGuid();
        var assetB = Guid.NewGuid();

        shelter.AddPicture(assetA, "first", Now);
        shelter.AddPicture(assetB, "second", Now);

        shelter.Pictures.Should().HaveCount(2);
        shelter.Pictures[0].SortOrder.Should().Be(0);
        shelter.Pictures[1].SortOrder.Should().Be(1);
    }

    [Fact]
    public void AddPicture_throws_when_asset_id_is_empty()
    {
        var shelter = ValidShelter();

        var act = () => shelter.AddPicture(Guid.Empty, null, Now);

        act.Should().Throw<DomainValidationException>().WithMessage("*asset*");
    }

    [Fact]
    public void RemovePicture_is_silent_when_picture_does_not_exist()
    {
        var shelter = ValidShelter();

        var act = () => shelter.RemovePicture(Guid.NewGuid(), Now.AddHours(1));

        act.Should().NotThrow();
        shelter.Pictures.Should().BeEmpty();
    }

    [Fact]
    public void Deactivate_is_idempotent_and_does_not_bump_timestamp_when_already_inactive()
    {
        var shelter = ValidShelter();
        var first = Now.AddHours(1);
        shelter.Deactivate(first);
        var second = Now.AddHours(2);

        shelter.Deactivate(second);

        shelter.IsActive.Should().BeFalse();
        shelter.UpdatedAt.Should().Be(first);
    }

    [Fact]
    public void Reactivate_flips_back_to_active()
    {
        var shelter = ValidShelter();
        shelter.Deactivate(Now.AddHours(1));
        var later = Now.AddHours(2);

        shelter.Reactivate(later);

        shelter.IsActive.Should().BeTrue();
        shelter.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void OwnedBy_matches_owner_id()
    {
        var shelter = ValidShelter();

        shelter.OwnedBy(OwnerId).Should().BeTrue();
        shelter.OwnedBy(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void AssertCanBeBooked_throws_when_shelter_is_inactive()
    {
        var shelter = ValidShelter();
        shelter.Deactivate(Now.AddHours(1));

        var act = () => shelter.AssertCanBeBooked(BookingType.Inclusive, 1);

        act.Should().Throw<DomainValidationException>().WithMessage("*not active*");
    }

    [Fact]
    public void AssertCanBeBooked_throws_when_inclusive_request_hits_exclusive_only_shelter()
    {
        var shelter = ValidShelter(policy: ShelterBookingPolicy.ExclusiveOnly);

        var act = () => shelter.AssertCanBeBooked(BookingType.Inclusive, 1);

        act.Should().Throw<DomainValidationException>().WithMessage("*exclusive*");
    }

    [Fact]
    public void AssertCanBeBooked_throws_when_exclusive_request_hits_inclusive_only_shelter()
    {
        var shelter = ValidShelter(policy: ShelterBookingPolicy.InclusiveOnly);

        var act = () => shelter.AssertCanBeBooked(BookingType.Exclusive, 1);

        act.Should().Throw<DomainValidationException>().WithMessage("*inclusive*");
    }

    [Fact]
    public void AssertCanBeBooked_throws_when_guest_count_exceeds_capacity()
    {
        var shelter = ValidShelter(capacity: 4);

        var act = () => shelter.AssertCanBeBooked(BookingType.Inclusive, 5);

        act.Should().Throw<DomainValidationException>().WithMessage("*capacity*");
    }

    [Fact]
    public void AssertCanBeBooked_passes_for_active_compatible_within_capacity()
    {
        var shelter = ValidShelter(capacity: 4, policy: ShelterBookingPolicy.Both);

        var act = () => shelter.AssertCanBeBooked(BookingType.Inclusive, 4);

        act.Should().NotThrow();
    }
}
