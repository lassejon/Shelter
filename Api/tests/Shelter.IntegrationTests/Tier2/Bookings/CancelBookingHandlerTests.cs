using App.Features.Bookings.Cancel;
using Microsoft.Extensions.DependencyInjection;
using Shelter.Domain.Common;
using Shelter.IntegrationTests.Infrastructure;

namespace Shelter.IntegrationTests.Tier2.Bookings;

/// <summary>
/// Proves the handler reads <see cref="App.Common.IClock.UtcNow"/> rather than
/// <c>DateTimeOffset.UtcNow</c>. If clock injection regressed (handler bypassed
/// <c>IClock</c>), this test would still pass at "real" clock time but become
/// non-deterministic — the assertion below relies on the test clock advancing past a
/// future <c>StartUtc</c> while the wall clock has not.
/// </summary>
public sealed class CancelBookingHandlerTests(PostgresFixture postgres) : HandlerTestBase(postgres)
{
    [Fact]
    public async Task Cancel_after_booking_started_throws()
    {
        var owner = await TestData.SeedUserAsync(Db);
        var booker = await TestData.SeedUserAsync(Db);
        var shelter = await TestData.SeedShelterAsync(Db, owner.Id);

        var seedToday = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var startUtc = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var endUtc = new DateTimeOffset(2026, 2, 5, 0, 0, 0, TimeSpan.Zero);

        var booking = await TestData.SeedBookingAsync(
            Db, shelter.Id, booker.Id, startUtc, endUtc, today: seedToday, now: seedToday);

        Clock.UtcNow = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

        var handler = Services.GetRequiredService<CancelBookingHandler>();

        var act = () => handler.HandleAsync(booking.Id, booker.Id, CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("*has started or is in the past*");
    }
}
