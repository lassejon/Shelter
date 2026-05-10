using Shelter.Domain.Bookings;
using Shelter.Domain.Common;
using Shelter.Domain.Shelters;

namespace Shelter.UnitTests.Domain.Bookings;

public class BookingTests
{
    private static readonly DateTimeOffset Today = new(2026, 5, 7, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 5, 7, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid ShelterId = Guid.NewGuid();
    private static readonly Guid BookerId = Guid.NewGuid();

    private static Booking ValidBooking(
        DateTimeOffset? startUtc = null,
        DateTimeOffset? endUtc = null,
        int guests = 2,
        BookingType type = BookingType.Inclusive) =>
        Booking.Create(
            ShelterId,
            BookerId,
            startUtc ?? Today.AddDays(3),
            endUtc ?? Today.AddDays(5),
            guests,
            type,
            BookingApprovalMode.Instant,
            Today,
            Now);

    [Fact]
    public void Create_returns_confirmed_booking_with_invariants_satisfied()
    {
        var booking = ValidBooking();

        booking.Id.Should().NotBe(Guid.Empty);
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.CreatedAt.Should().Be(Now);
        booking.UpdatedAt.Should().Be(Now);
    }

    [Fact]
    public void Create_throws_when_start_is_in_the_past()
    {
        var act = () => ValidBooking(startUtc: Today.AddDays(-1), endUtc: Today.AddDays(1));

        act.Should().Throw<DomainValidationException>().WithMessage("*future*");
    }

    [Fact]
    public void Create_allows_start_today()
    {
        var act = () => ValidBooking(startUtc: Today, endUtc: Today.AddDays(1));

        act.Should().NotThrow();
    }

    [Fact]
    public void Create_throws_when_end_is_not_after_start()
    {
        var start = Today.AddDays(3);

        var act = () => ValidBooking(startUtc: start, endUtc: start);

        act.Should().Throw<DomainValidationException>().WithMessage("*after*");
    }

    [Fact]
    public void Create_throws_when_duration_exceeds_365_nights()
    {
        var start = Today.AddDays(1);

        var act = () => ValidBooking(startUtc: start, endUtc: start.AddDays(366));

        act.Should().Throw<DomainValidationException>().WithMessage("*365*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_throws_when_guests_is_not_positive(int guests)
    {
        var act = () => ValidBooking(guests: guests);

        act.Should().Throw<DomainValidationException>().WithMessage("*positive*");
    }

    [Fact]
    public void Create_throws_when_type_is_undefined()
    {
        var act = () => ValidBooking(type: (BookingType)99);

        act.Should().Throw<DomainValidationException>().WithMessage("*booking type*");
    }

    [Fact]
    public void Cancel_marks_booking_cancelled_and_bumps_timestamp()
    {
        var booking = ValidBooking(startUtc: Today.AddDays(3));
        var nowBeforeStart = Today.AddDays(1);

        booking.Cancel(nowBeforeStart);

        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.UpdatedAt.Should().Be(nowBeforeStart);
    }

    [Fact]
    public void Cancel_throws_when_booking_has_already_started()
    {
        var booking = ValidBooking(startUtc: Today.AddDays(2), endUtc: Today.AddDays(5));
        var nowAfterStart = Today.AddDays(3);

        var act = () => booking.Cancel(nowAfterStart);

        act.Should().Throw<DomainValidationException>().WithMessage("*started*");
    }

    [Fact]
    public void Cancel_throws_when_booking_starts_exactly_now()
    {
        var start = Today.AddDays(2);
        var booking = ValidBooking(startUtc: start, endUtc: start.AddDays(1));

        var act = () => booking.Cancel(start);

        act.Should().Throw<DomainValidationException>().WithMessage("*started*");
    }

    [Fact]
    public void Cancel_throws_when_already_cancelled()
    {
        var booking = ValidBooking(startUtc: Today.AddDays(3));
        booking.Cancel(Today.AddDays(1));

        var act = () => booking.Cancel(Today.AddDays(2));

        act.Should().Throw<DomainValidationException>().WithMessage("*already*");
    }

    [Fact]
    public void Confirm_throws_when_booking_is_cancelled()
    {
        var booking = ValidBooking(startUtc: Today.AddDays(3));
        booking.Cancel(Today.AddDays(1));

        var act = () => booking.Confirm(Today.AddDays(2));

        act.Should().Throw<DomainValidationException>().WithMessage("*cancelled*");
    }

    [Fact]
    public void Confirm_is_idempotent_when_already_confirmed()
    {
        var booking = ValidBooking(startUtc: Today.AddDays(3));
        var initialUpdated = booking.UpdatedAt;

        booking.Confirm(Now.AddHours(5));

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.UpdatedAt.Should().Be(initialUpdated);
    }

    [Fact]
    public void Reschedule_updates_dates_and_bumps_timestamp()
    {
        var booking = ValidBooking(startUtc: Today.AddDays(3), endUtc: Today.AddDays(5));
        var newStart = Today.AddDays(10);
        var newEnd = Today.AddDays(12);
        var rescheduledAt = Now.AddHours(1);

        booking.Reschedule(newStart, newEnd, rescheduledAt);

        booking.StartUtc.Should().Be(newStart);
        booking.EndUtc.Should().Be(newEnd);
        booking.UpdatedAt.Should().Be(rescheduledAt);
    }

    [Fact]
    public void Reschedule_throws_when_cancelled()
    {
        var booking = ValidBooking(startUtc: Today.AddDays(3));
        booking.Cancel(Today.AddDays(1));

        var act = () => booking.Reschedule(Today.AddDays(10), Today.AddDays(12), Now.AddDays(2));

        act.Should().Throw<DomainValidationException>().WithMessage("*cancelled*");
    }

    [Fact]
    public void Reschedule_throws_when_new_range_is_invalid()
    {
        var booking = ValidBooking(startUtc: Today.AddDays(3));
        var newStart = Today.AddDays(10);

        var act = () => booking.Reschedule(newStart, newStart, Now.AddHours(1));

        act.Should().Throw<DomainValidationException>().WithMessage("*after*");
    }

    [Fact]
    public void BookedBy_matches_booker_id()
    {
        var booking = ValidBooking();

        booking.BookedBy(BookerId).Should().BeTrue();
        booking.BookedBy(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void Create_starts_pending_when_shelter_requires_approval()
    {
        var booking = Booking.Create(
            ShelterId,
            BookerId,
            Today.AddDays(3),
            Today.AddDays(5),
            2,
            BookingType.Inclusive,
            BookingApprovalMode.RequiresApproval,
            Today,
            Now);

        booking.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public void Confirm_transitions_pending_to_confirmed()
    {
        var booking = Booking.Create(
            ShelterId,
            BookerId,
            Today.AddDays(3),
            Today.AddDays(5),
            2,
            BookingType.Inclusive,
            BookingApprovalMode.RequiresApproval,
            Today,
            Now);
        var approvedAt = Now.AddHours(1);

        booking.Confirm(approvedAt);

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.UpdatedAt.Should().Be(approvedAt);
    }

    [Fact]
    public void Confirm_throws_when_booking_has_already_started()
    {
        var booking = Booking.Create(
            ShelterId,
            BookerId,
            Today.AddDays(2),
            Today.AddDays(4),
            2,
            BookingType.Inclusive,
            BookingApprovalMode.RequiresApproval,
            Today,
            Now);
        var nowAfterStart = Today.AddDays(3);

        var act = () => booking.Confirm(nowAfterStart);

        act.Should().Throw<DomainValidationException>().WithMessage("*started*");
    }
}
