using App.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Assets;
using Shelter.Domain.Bookings;
using Shelter.Domain.Reviews;
using Shelter.Domain.Users;
using ShelterEntity = Shelter.Domain.Shelters.Shelter;

namespace Shelter.Infrastructure.Persistence;

public sealed class ShelterDbContext(DbContextOptions<ShelterDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options), IShelterDbContext
{
    public DbSet<ShelterEntity> Shelters => Set<ShelterEntity>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Asset> Assets => Set<Asset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShelterDbContext).Assembly);

        // ── Map App.Persistence.TextFunctions stubs → PostgreSQL pg_trgm SQL functions ──
        //
        // The C# methods on TextFunctions throw if called at runtime; EF Core never invokes
        // them. Inside an IQueryable LINQ tree the call appears as an Expression node, and
        // these bindings tell the SQL translator: "replace this method call with a call to
        // the SQL function named X". Plain IEnumerable LINQ skips translation and would
        // invoke the body — the throw is the misuse guardrail.
        //
        // pg_trgm (the EXTENSION, installed by the `EnableTrigramAndNameIndex` migration via
        // `CREATE EXTENSION pg_trgm`) is not the same thing as `similarity` (a FUNCTION the
        // extension provides). EF emits the function name only; Postgres resolves it through
        // the search_path (default `public`), where pg_trgm installs its functions. Mental
        // model: pg_trgm is the NuGet package, `similarity` is a static method inside it. EF
        // does not need to know about extensions — it just needs the function name.

        // HasDbFunction(MethodInfo) → register the C#-method-to-SQL-function binding.
        //   typeof(...).GetMethod(nameof(...))! → reflect the static method; nameof keeps
        //   the string in sync with the symbol, and `!` is safe because nameof guarantees
        //   the method exists at compile time.
        // HasName(string) → the SQL function name to emit at translation. A bare
        //   `similarity(a, b)` in the generated SQL resolves through Postgres' search_path
        //   to the pg_trgm function.
        modelBuilder
            .HasDbFunction(typeof(TextFunctions)
                .GetMethod(nameof(TextFunctions.TrigramSimilarity))!)
            .HasName("similarity");

        // word_similarity is a different pg_trgm function (asymmetric, short-vs-long match);
        // see SearchShelterHandler for why one is used for names and the other for descriptions.
        modelBuilder
            .HasDbFunction(typeof(TextFunctions)
                .GetMethod(nameof(TextFunctions.TrigramWordSimilarity))!)
            .HasName("word_similarity");
    }
}
