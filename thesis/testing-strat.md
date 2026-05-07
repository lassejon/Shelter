# Testing Strategy

The backend's test surface is split into three tiers. This document records what each tier covers, the tooling that supports it, and the architectural decisions made to keep each tier honest. Tiers 1 and 1.5 are implemented; Tiers 2 and 3 are scoped here but deferred — the milestones at which they pay off are called out below.

## The three-tier shape

| Tier | Project | Covers | Speed |
|---|---|---|---|
| 1 — domain unit | `Shelter.UnitTests` | aggregate invariants, state transitions, validation | µs / test |
| 1.5 — algorithm unit | `Shelter.UnitTests` | the booking-availability sweepline, lifted out of handler bodies | µs / test |
| 2 — handler integration | `Shelter.IntegrationTests` (planned) | handler ↔ real Postgres ↔ real EF; no HTTP | ms / test |
| 3 — API integration | `Shelter.IntegrationTests` (planned) | full HTTP via `WebApplicationFactory` | ms / test |

The defining principle: **each tier should be the cheapest layer that catches the class of bug it owns**. Domain invariant bugs are caught in microseconds by Tier 1 — pushing them down to Tier 3 means each invariant violation costs a database round-trip and an HTTP cycle to detect. Conversely, EF translation bugs (a typo in a `[DbFunction]` mapping, a `HasColumnType` missing on a Postgres-specific column, a forgotten `OwnsOne`) only surface when EF actually emits SQL — so trying to test them with Tier 1 fakes is theatre, and the only honest tier for them is Tier 2.

## Why no in-memory database tier

The textbook .NET advice is "use `Microsoft.EntityFrameworkCore.InMemory` for unit-testable handlers". The project rejects this because of one specific Postgres dependency.

`Shelter.Infrastructure/Persistence/ShelterDbContext.cs` registers two `[DbFunction]` mappings:

```csharp
modelBuilder.HasDbFunction(typeof(TextFunctions).GetMethod(nameof(TextFunctions.TrigramSimilarity))!)
    .HasName("similarity");
modelBuilder.HasDbFunction(typeof(TextFunctions).GetMethod(nameof(TextFunctions.TrigramWordSimilarity))!)
    .HasName("word_similarity");
```

These translate calls inside `IQueryable<T>.Where(...)` to PostgreSQL's `pg_trgm` `similarity()` and `word_similarity()` functions. The InMemory provider has no SQL pipeline, so the translation is never attempted; the body of `TextFunctions.TrigramSimilarity` (which is `throw new InvalidOperationException(...)`) runs at materialisation time and the test crashes on a confusing exception. Adding "make these methods return real values when called outside EF" to the codebase to keep InMemory happy would mean the production code carries a fallback path that exists only for tests — exactly the kind of test-driven distortion the strategy avoids.

SQLite-in-memory has the same problem from the opposite angle: it has a SQL pipeline, but no `similarity()` function, and even if we registered a UDF the semantics would not match Postgres's. Tests would pass against SQLite that fail against Postgres.

The honest answer is **Testcontainers.PostgreSql** for any test that needs the database. That work belongs to Tier 2 and is deferred until a real handler-level regression motivates it.

## Tooling decisions

| Tool | Picked | Why |
|---|---|---|
| Test runner | **xUnit 2.9** | The default in modern .NET. xUnit v3 is stable but the v2 ecosystem (analyzers, IDE plugins, `xunit.runner.visualstudio`) is still the path of least friction. Migrate when v3 becomes the template default. |
| Assertions | **AwesomeAssertions 9** | Community fork of FluentAssertions, kept on the original MIT licence. FluentAssertions itself moved to a paid commercial licence at v8 (Xceed acquisition); AwesomeAssertions exists specifically to keep the old API freely usable. The DSL is identical (`x.Should().Be(...)`, `act.Should().Throw<T>().WithMessage("*...*")`). |
| Mocking library | **None at Tier 1** | All ambient deps the domain might want (`IClock`, `IFileStorage`) are excluded from the domain by design — domain methods take primitives like `DateTimeOffset now` as parameters. Tier 2 will introduce hand-rolled fakes (5-line classes) for the same reason: they read better than mock setup and don't lie about EF the way `Mock<DbSet<T>>` does. |
| Coverage | `coverlet.collector` (default from the xUnit template) | Collected on-demand. No coverage threshold enforced; the goal is "every business rule has a test that would fail if the rule were broken", which is more meaningful than a percentage gate. |

The repo currently builds against .NET 10.0 and the test project targets the same TFM. Package versions: `Microsoft.NET.Test.Sdk 17.14`, `xunit 2.9.3`, `xunit.runner.visualstudio 3.1.4`, `AwesomeAssertions 9.4.0`, `coverlet.collector 6.0.4`.

## Tier 1 — domain unit tests

Coverage target: every public method on every aggregate that takes domain logic decisions.

| Aggregate | Test file | What's covered |
|---|---|---|
| `Shelter` | `Domain/Shelters/ShelterTests.cs` | `Create` (name / capacity / coords / policy validation), `UpdateDetails`, `Relocate`, `AddPicture` (asset-id guard, sort-order assignment), `RemovePicture`, `Deactivate` / `Reactivate` idempotence, `OwnedBy`, `AssertCanBeBooked` (inactive, policy mismatch, guest overflow). |
| `Booking` | `Domain/Bookings/BookingTests.cs` | `Create` (past-date, end-after-start, max nights, guests, type enum), `Cancel` (already-started, double-cancel), `Confirm` (cancelled, idempotence), `Reschedule` (cancelled, range), `BookedBy`. |
| `Review` | `Domain/Reviews/ReviewTests.cs` | `Create` (rating enum), `Edit`, `AddPicture` (asset-id, sort-order), `RemovePicture` no-op, `WrittenBy`. |

Each test is 5–15 lines. Setup is a shared `Now` constant + a `ValidXxx()` helper that returns an aggregate at its happiest path; tests then exercise one specific deviation. There's no test data builder DSL — at this scale, three lines of inline setup beat introducing AutoFixture / Bogus.

Test naming uses descriptive sentences, snake_case: `Cancel_throws_when_booking_has_already_started`, `Peak_treats_booking_end_as_exclusive_boundary`. The `Method_Scenario_Expected` shape is implicit but the underscores do the structural work; reading the test list as a spec is the goal.

**What's deliberately not tested at Tier 1.** Setter behaviour on properties with `private set` (testing that `_pictures` is empty after construction is testing the field initialiser, not domain logic). Constructor argument forwarding (`Create` writes `Id = Guid.NewGuid()` — there's no scenario in which this could be wrong without a compiler error). EF configuration (`Pictures` navigation, `OnDelete` cascades) — that's Tier 2's job.

## Tier 1.5 — sweepline extraction

The first concrete payoff of writing tests was discovering — and removing — a piece of duplicated business logic.

### Before

The booking-availability check ("does this candidate booking fit given the existing overlapping bookings?") was implemented twice:

1. `SearchShelterHandler.FilterByAvailabilityAsync` — used by the search filter to drop shelters that won't fit a date+guests query. Returned `bool` per shelter.
2. `CreateBookingHandler.AssertNoConflicts` — used at booking creation. Threw `DomainValidationException` on conflict.

Both implemented the same algorithm: detect any exclusive-overlap (which blocks the entire shelter regardless of capacity), otherwise sweep candidate moments to find peak concurrent inclusive guests, compare against capacity. The two copies had drifted in subtle ways — the search version computed peak across *all* overlapping bookings (which it could get away with because it had already early-returned on any exclusive overlap), the create-booking version used a simpler "sum of inclusive guests" approximation that over-estimates peak when bookings don't overlap each other but both overlap the candidate.

Both copies were buried inside async, EF-querying methods. Unit-testing either without the database in scope was impossible without first extracting them.

### The extraction

Two domain primitives were added to `Shelter.Domain.Shelters.Shelter`:

```csharp
public void AssertCanFit(IReadOnlyList<BookingPeriod> overlapping, BookingPeriod candidate);

public static int PeakInclusiveGuests(
    IReadOnlyList<BookingPeriod> overlapping,
    DateTimeOffset windowStartUtc,
    DateTimeOffset windowEndUtc);
```

Plus a small value type in `Shelter.Domain.Bookings`:

```csharp
public readonly record struct BookingPeriod(
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    int Guests,
    BookingType Type);
```

`AssertCanFit` is the entry point for the create-booking flow — it composes `AssertCanBeBooked` (the single-booking type/policy/capacity check that already lived on the entity) with the overlap and sweepline checks, throwing `DomainValidationException` with a message that names the actual remaining capacity.

`PeakInclusiveGuests` is the algorithmic primitive — pure, static, no `this`. It exists separately because the search use case needs the *peak number* (so it can compose its own "leaves room for N" predicate without going through an exception path), whereas the create use case needs the *throw on overflow* assertion. Sharing the algorithm without sharing the assertion shape keeps both call sites honest about what they actually want.

### Why on the entity, not in a static helper class

`CLAUDE.md` (`Api/CLAUDE.md` § "Domain logic lives on the entity") frames this as the default: **the entity is the contract between slices**. Putting the rule inside `Shelter` means a future slice that mutates the booking schedule cannot accidentally bypass it — there is one place the algorithm lives, and the type system funnels callers there. The entity already owned `AssertCanBeBooked`; adding `AssertCanFit` keeps the surface coherent.

The static `PeakInclusiveGuests` is the one concession to "pure algorithm" — it doesn't need any of `Shelter`'s state, but it lives on the type because the type is the natural namespace. `BookingAvailability` as a separate static class would have worked too; the choice between them was a stylistic preference, not a load-bearing decision.

### Why a `BookingPeriod` value type rather than reusing `Booking`

The aggregates intentionally have nothing public the EF projection could populate. `Booking` has a private parameterless ctor and private setters; constructing one in tests requires the `Create` factory, which validates all the business rules and forces the test to thread valid dates and guest counts through every scenario. That's right for testing `Booking.Cancel`, but it's wrong for testing the sweepline — a sweepline test should be free to assert behaviour at "what if a booking started in the past" without re-litigating `Booking.Create`'s past-date guard.

A small `record struct` of just the fields the sweepline reads cuts the test surface to exactly what the algorithm cares about: a window, a guest count, and a type. It's also what the EF projection materialises (`.Select(b => new BookingPeriod(b.StartUtc, b.EndUtc, b.Guests, b.Type))`), so the algorithm sees the same shape in production and in tests.

### After

`SearchShelterHandler.FilterByAvailabilityAsync` is now a thin EF projection plus a per-shelter predicate that calls `Shelter.PeakInclusiveGuests`. `CreateBookingHandler` lost its private `AssertNoConflicts` method entirely; the body is now a single `shelter.AssertCanFit(overlapping, candidate)` call.

Sweepline coverage lives in `Domain/Shelters/BookingAvailabilityTests.cs`:

- `PeakInclusiveGuests`: empty, exclusive-only (filtered to zero), single inclusive, overlapping inclusive (concurrent peak), non-overlapping inclusive (max not sum), end-of-booking boundary, window-end boundary.
- `AssertCanFit`: no overlap within capacity, candidate exceeds total capacity, candidate exclusive vs existing overlap, existing exclusive blocks any inclusive, inclusive overlap leaves room, inclusive peak overflow, concurrent-peak vs total-sum (the bug the duplicated implementations differed on), inactive shelter, policy mismatch.

Sixteen tests, ~150 lines, run in under 2 ms. The same coverage at Tier 2 would be sixteen Postgres round-trips per run.

## Tier 2 — handler integration (deferred)

Scope when written: one Postgres container per test run via Testcontainers, fresh DI scope per test, `Respawn` truncates user-data tables between tests, `IFileStorage` swapped for an in-memory dictionary, `IClock` swapped for a fixed clock. Test base class (`IntegrationTestBase`) exposes `Db` (typed as `IShelterDbContext`) and helper methods to seed users via the real `UserManager<User>` — auth tests should exercise the actual auth wiring, not a `TestAuthHandler` shortcut.

Coverage targets:
- `SearchShelterHandler` end-to-end — proves `pg_trgm` actually translates and that the bbox filter, capacity prefilter, and availability sweepline compose correctly.
- `CreateShelterHandler` — multipart picture upload writes assets, sort order is preserved, blob keys round-trip.
- `CreateBookingHandler` — happy path, capacity overflow rejected (with the exact message the domain emits), exclusive-overlap rejected, post-cancel slot reuse.
- `CancelBookingHandler` — rejects already-started bookings (proves clock injection works).
- `CreateReviewHandler` — first review succeeds, second by same user fails on the `(ShelterId, ReviewerId)` unique constraint (proves the DB constraint matches the application check).
- `MeHandler` / `UpgradeToOwnerHandler` — re-issued JWT contains updated roles.

Trigger to start: a real handler-level regression that would not have been caught by Tier 1. Speculative motivations ("we should have integration tests") are deliberately not enough — the risk is that 80 % of the integration tests we'd write today end up restating Tier 1 expectations in a slower form.

## Tier 3 — API integration (deferred)

Same fixture as Tier 2 plus `WebApplicationFactory<Program>`. One happy + one auth/error per endpoint, ~30 tests total. Auth uses real `UserManager<User>` to create users + real `IJwtGenerator` to sign tokens; no `TestAuthHandler` bypass.

The single most valuable Tier 3 test would be the `extensions.errors: string[]` shape on register: it's the contract the frontend explicitly depends on, it's a custom `ProblemDetails` extension that no domain or handler test would catch a regression in, and a future "let's just join the errors into Detail" cleanup would silently break the UI. When Tier 3 lands, that test goes in first.

## What's not tested at any tier

- Endpoint route registration. The Minimal API `MapXxx` calls are mechanical wiring; misconfiguration surfaces on first manual hit.
- DI registration. Errors surface at app start.
- ASP.NET Identity password rules. Already covered by Microsoft's tests.
- `OnModelCreating` metadata correctness. The first migration that runs against an empty DB catches misconfigured `[DbFunction]` mappings, missing `HasColumnType`, etc. Adding a Tier 2 "applies migrations cleanly" test would re-test what `dotnet ef database update` already does in the test fixture's setup.
- pg_trgm threshold values. The 0.1 / 0.25 floors are UX tuning, not correctness invariants — they were lowered after a real query failed to surface a real shelter, and they'll move again when a future case argues for it. Asserting the exact value would freeze the tuning into a test name.

## Decisions to revisit

- **Coverage threshold.** None today. Add a 70 % gate when there's a CI pipeline to enforce it on, and only if regressions start landing that "would have been caught by a coverage line".
- **Mocking library.** None at Tier 1. If Tier 2/3 introduce more than two ambient interfaces that need stubbing, prefer NSubstitute over Moq (Moq's 2023 SponsorLink incident soured the .NET community on it; NSubstitute has a cleaner API and a clean licence).
- **Snapshot-test the OpenAPI document.** When the FE team starts pinning to specific contract behaviour, a snapshot of `Api/openapi/Shelter.Api.json` against a committed baseline would catch accidental breaking changes earlier than a frontend type error.
- **Property-based tests for the sweepline.** The current 16 sweepline tests are example-based. FsCheck or CsCheck against `(IReadOnlyList<BookingPeriod>, BookingPeriod)` could probably surface the boundary edge-case I missed first attempt — but only worth the dependency if the sweepline grows past its current shape.
