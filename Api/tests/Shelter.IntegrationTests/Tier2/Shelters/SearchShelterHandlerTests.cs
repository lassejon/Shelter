using App.Features.Shelters.Search;
using Microsoft.Extensions.DependencyInjection;
using Shelter.IntegrationTests.Infrastructure;

namespace Shelter.IntegrationTests.Tier2.Shelters;

/// <summary>
/// Proves the <c>[DbFunction]</c> mapping in <c>ShelterDbContext.OnModelCreating</c> translates to
/// real Postgres <c>similarity()</c> / <c>word_similarity()</c> calls. No Tier 1 fake catches a
/// regression here — the C# stubs throw at runtime; only EF's SQL pipeline against real Postgres
/// (with the <c>pg_trgm</c> extension installed by the <c>EnableTrigramAndNameIndex</c> migration)
/// can verify the round-trip.
/// </summary>
public sealed class SearchShelterHandlerTests(PostgresFixture postgres) : HandlerTestBase(postgres)
{
    [Fact]
    public async Task Trigram_match_finds_shelter_with_typo_in_query()
    {
        var owner = await TestData.SeedUserAsync(Db);
        await TestData.SeedShelterAsync(Db, owner.Id, name: "Birch Hut");
        await TestData.SeedShelterAsync(Db, owner.Id, name: "Oak Cabin");
        await TestData.SeedShelterAsync(Db, owner.Id, name: "Pine Lodge");

        var handler = Services.GetRequiredService<SearchShelterHandler>();

        var results = await handler.HandleAsync(
            new SearchShelterRequest { Q = "birsh" },
            CancellationToken.None);

        results.Should().ContainSingle()
            .Which.Name.Should().Be("Birch Hut");
    }

    [Fact]
    public async Task Description_word_similarity_matches_feature_query()
    {
        var owner = await TestData.SeedUserAsync(Db);
        await TestData.SeedShelterAsync(Db, owner.Id,
            name: "Riverside",
            description: "A cozy spot by the lake with a wood-burning fireplace and a private pier.");
        await TestData.SeedShelterAsync(Db, owner.Id,
            name: "Hilltop",
            description: "Panoramic mountain views, nothing else nearby.");

        var handler = Services.GetRequiredService<SearchShelterHandler>();

        var results = await handler.HandleAsync(
            new SearchShelterRequest { Q = "fireplace" },
            CancellationToken.None);

        results.Should().ContainSingle()
            .Which.Name.Should().Be("Riverside");
    }
}
