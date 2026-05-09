using App.Common;

namespace Shelter.IntegrationTests.Infrastructure;

/// <summary>
/// Settable <see cref="IClock"/> for tests that need to control "now" — e.g. cancelling a
/// booking after its <c>StartUtc</c> has passed.
/// </summary>
public sealed class TestClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
}
