using App.Common;
using App.Features.Reviews.Create;
using Microsoft.Extensions.DependencyInjection;
using Shelter.Domain.Common;
using Shelter.Domain.Reviews;
using Shelter.IntegrationTests.Infrastructure;

namespace Shelter.IntegrationTests.Tier2.Reviews;

/// <summary>
/// The "one review per (shelter, reviewer)" invariant has two enforcement layers:
/// the handler's <c>AnyAsync</c> pre-check (throws <see cref="DomainValidationException"/>)
/// and a Postgres unique index on <c>(ShelterId, ReviewerId)</c> as a backstop. This test
/// covers the application path; the DB constraint is verified at migration time
/// (<c>UniqueReviewPerUserPerShelter</c>).
/// </summary>
public sealed class CreateReviewHandlerTests(PostgresFixture postgres) : HandlerTestBase(postgres)
{
    [Fact]
    public async Task Second_review_by_same_user_for_same_shelter_throws()
    {
        var owner = await TestData.SeedUserAsync(Db);
        var reviewer = await TestData.SeedUserAsync(Db);
        var shelter = await TestData.SeedShelterAsync(Db, owner.Id);
        await TestData.SeedReviewAsync(Db, shelter.Id, reviewer.Id);

        var handler = Services.GetRequiredService<CreateReviewHandler>();

        var act = () => handler.HandleAsync(
            shelter.Id,
            new CreateReviewRequest { Rating = Rating.Good, Comment = "Second attempt" },
            reviewer.Id,
            pictures: Array.Empty<FileUpload>(),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("*already reviewed*");
    }
}
