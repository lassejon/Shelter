using Shelter.Domain.Bookings;
using Shelter.Domain.Common;
using Shelter.Domain.Shelters;
using ShelterEntity = Shelter.Domain.Shelters.Shelter;

namespace Shelter.UnitTests.Domain.Shelters;

/// <summary>
/// Sweepline algorithm tests. Lifted out of <c>SearchShelterHandler</c> /
/// <c>CreateBookingHandler</c> when the duplicated availability check became its own primitive on
/// <see cref="ShelterEntity"/>. Pure logic — no DB, no clock, no DI.
/// </summary>
public class BookingAvailabilityTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 7, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset WindowStart = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset WindowEnd = new(2026, 6, 5, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid OwnerId = Guid.NewGuid();

    private static ShelterEntity ShelterWith(int capacity, ShelterBookingPolicy policy = ShelterBookingPolicy.Both) =>
        ShelterEntity.Create(OwnerId, "Hut", null, capacity, 0, 0, policy, BookingApprovalMode.Instant, Now);

    private static BookingPeriod Inclusive(DateTimeOffset start, DateTimeOffset end, int guests) =>
        new(start, end, guests, BookingType.Inclusive);

    private static BookingPeriod Exclusive(DateTimeOffset start, DateTimeOffset end, int guests = 1) =>
        new(start, end, guests, BookingType.Exclusive);

    // ---------- PeakInclusiveGuests ----------

    [Fact]
    public void Peak_is_zero_when_no_bookings()
    {
        var peak = ShelterEntity.PeakInclusiveGuests([], WindowStart, WindowEnd);

        peak.Should().Be(0);
    }

    [Fact]
    public void Peak_is_zero_when_only_exclusive_bookings_are_present()
    {
        // Exclusive bookings are filtered out — caller is expected to short-circuit on exclusive
        // overlap separately. The sweepline only cares about inclusive guest counts.
        var bookings = new[] { Exclusive(WindowStart, WindowEnd) };

        var peak = ShelterEntity.PeakInclusiveGuests(bookings, WindowStart, WindowEnd);

        peak.Should().Be(0);
    }

    [Fact]
    public void Peak_returns_guests_of_a_single_inclusive_booking_covering_the_window()
    {
        var bookings = new[] { Inclusive(WindowStart, WindowEnd, 3) };

        var peak = ShelterEntity.PeakInclusiveGuests(bookings, WindowStart, WindowEnd);

        peak.Should().Be(3);
    }

    [Fact]
    public void Peak_sums_overlapping_inclusive_bookings_at_the_concurrent_moment()
    {
        // [B1: 1 guest from Day1–Day3] [B2: 2 guests from Day2–Day4] — peak is 3 guests on Day2.
        var bookings = new[]
        {
            Inclusive(WindowStart, WindowStart.AddDays(2), 1),
            Inclusive(WindowStart.AddDays(1), WindowStart.AddDays(3), 2),
        };

        var peak = ShelterEntity.PeakInclusiveGuests(bookings, WindowStart, WindowEnd);

        peak.Should().Be(3);
    }

    [Fact]
    public void Peak_finds_the_concurrent_max_not_the_total_sum()
    {
        // Two non-overlapping inclusive bookings: peak is the larger of the two, not the sum.
        var bookings = new[]
        {
            Inclusive(WindowStart, WindowStart.AddDays(1), 4),
            Inclusive(WindowStart.AddDays(2), WindowStart.AddDays(3), 2),
        };

        var peak = ShelterEntity.PeakInclusiveGuests(bookings, WindowStart, WindowEnd);

        peak.Should().Be(4);
    }

    [Fact]
    public void Peak_treats_booking_end_as_exclusive_boundary()
    {
        // A booking ending exactly at the start of the next is not concurrent — half-open intervals.
        var bookings = new[]
        {
            Inclusive(WindowStart, WindowStart.AddDays(1), 2),
            Inclusive(WindowStart.AddDays(1), WindowStart.AddDays(2), 3),
        };

        var peak = ShelterEntity.PeakInclusiveGuests(bookings, WindowStart, WindowEnd);

        peak.Should().Be(3);
    }

    [Fact]
    public void Peak_ignores_bookings_starting_exactly_at_window_end()
    {
        // A booking that starts at WindowEnd is outside the half-open window [Start, End).
        var bookings = new[]
        {
            Inclusive(WindowEnd, WindowEnd.AddDays(1), 5),
        };

        var peak = ShelterEntity.PeakInclusiveGuests(bookings, WindowStart, WindowEnd);

        peak.Should().Be(0);
    }

    // ---------- AssertCanFit ----------

    [Fact]
    public void AssertCanFit_passes_when_no_overlapping_bookings_and_within_capacity()
    {
        var shelter = ShelterWith(capacity: 4);
        var candidate = Inclusive(WindowStart, WindowEnd, 4);

        var act = () => shelter.AssertCanFit([], candidate);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertCanFit_throws_when_candidate_exceeds_total_capacity_even_with_no_overlaps()
    {
        // Even on an empty calendar, requesting more guests than the shelter holds is rejected by
        // AssertCanBeBooked before the sweepline runs.
        var shelter = ShelterWith(capacity: 4);
        var candidate = Inclusive(WindowStart, WindowEnd, 5);

        var act = () => shelter.AssertCanFit([], candidate);

        act.Should().Throw<DomainValidationException>().WithMessage("*capacity*");
    }

    [Fact]
    public void AssertCanFit_throws_when_candidate_is_exclusive_and_overlap_exists()
    {
        var shelter = ShelterWith(capacity: 4);
        var existing = new[] { Inclusive(WindowStart, WindowEnd, 1) };
        var candidate = Exclusive(WindowStart, WindowEnd);

        var act = () => shelter.AssertCanFit(existing, candidate);

        act.Should().Throw<DomainValidationException>().WithMessage("*exclusive*");
    }

    [Fact]
    public void AssertCanFit_throws_when_overlap_includes_an_exclusive_booking()
    {
        var shelter = ShelterWith(capacity: 10);
        var existing = new[] { Exclusive(WindowStart, WindowEnd) };
        var candidate = Inclusive(WindowStart, WindowEnd, 1);

        var act = () => shelter.AssertCanFit(existing, candidate);

        act.Should().Throw<DomainValidationException>().WithMessage("*exclusively booked*");
    }

    [Fact]
    public void AssertCanFit_passes_when_inclusive_overlap_leaves_room_for_candidate()
    {
        var shelter = ShelterWith(capacity: 4);
        var existing = new[] { Inclusive(WindowStart, WindowEnd, 2) };
        var candidate = Inclusive(WindowStart, WindowEnd, 2);

        var act = () => shelter.AssertCanFit(existing, candidate);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertCanFit_throws_when_inclusive_peak_plus_candidate_exceeds_capacity()
    {
        var shelter = ShelterWith(capacity: 4);
        var existing = new[] { Inclusive(WindowStart, WindowEnd, 3) };
        var candidate = Inclusive(WindowStart, WindowEnd, 2);

        var act = () => shelter.AssertCanFit(existing, candidate);

        // `Capacity (4) - peak (3) = 1` slot remaining is reflected in the message.
        act.Should().Throw<DomainValidationException>().WithMessage("*Insufficient*1 spots*");
    }

    [Fact]
    public void AssertCanFit_uses_concurrent_peak_not_total_sum_of_overlaps()
    {
        // Two non-overlapping inclusive bookings of 3 each — naïve sum says 6 (over capacity), but
        // the actual concurrent peak is 3, leaving room for a 1-guest candidate.
        var shelter = ShelterWith(capacity: 4);
        var existing = new[]
        {
            Inclusive(WindowStart, WindowStart.AddDays(1), 3),
            Inclusive(WindowStart.AddDays(2), WindowStart.AddDays(3), 3),
        };
        var candidate = Inclusive(WindowStart, WindowEnd, 1);

        var act = () => shelter.AssertCanFit(existing, candidate);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertCanFit_throws_when_shelter_is_inactive()
    {
        var shelter = ShelterWith(capacity: 4);
        shelter.Deactivate(Now.AddHours(1));

        var act = () => shelter.AssertCanFit([], Inclusive(WindowStart, WindowEnd, 1));

        act.Should().Throw<DomainValidationException>().WithMessage("*not active*");
    }

    [Fact]
    public void AssertCanFit_throws_when_candidate_type_violates_booking_policy()
    {
        var shelter = ShelterWith(capacity: 4, policy: ShelterBookingPolicy.ExclusiveOnly);

        var act = () => shelter.AssertCanFit([], Inclusive(WindowStart, WindowEnd, 1));

        act.Should().Throw<DomainValidationException>().WithMessage("*exclusive*");
    }
}
