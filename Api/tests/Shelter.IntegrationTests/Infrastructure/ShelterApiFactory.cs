using App.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shelter.IntegrationTests.Infrastructure;

/// <summary>
/// Tier 3 host: real <c>Program.cs</c> middleware pipeline (auth, exception handler,
/// route mapping) on top of the shared Postgres container. Swaps <see cref="IFileStorage"/>
/// for an in-memory fake (so <c>BlobContainerClient</c> never tries to connect) and
/// <see cref="IClock"/> for a settable one.
///
/// Runs as <c>Development</c> so <c>appsettings.Development.json</c> supplies <c>Jwt:*</c> /
/// <c>BlobStorage:*</c> at <c>AddInfrastructure</c> time. <c>AddSettings&lt;JwtSettings&gt;</c>
/// binds eagerly inside <c>AddInfrastructure</c>, before the test fixture's
/// <c>ConfigureAppConfiguration</c> in-memory provider applies — so Jwt config has to be in
/// the bootstrap configuration. <c>AddDbContext</c>'s connection-string read is lazy (inside
/// the per-scope lambda), so the in-memory override below wins for the database. The dev-only
/// startup block in <c>Program.cs</c> (migrate + role seed + Swagger) runs idempotently
/// against the testcontainer.
/// </summary>
public sealed class ShelterApiFactory(PostgresFixture postgres) : WebApplicationFactory<Program>
{
    public TestClock TestClock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = postgres.ConnectionString,
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IFileStorage>();
            services.AddSingleton<IFileStorage, InMemoryFileStorage>();

            services.RemoveAll<IClock>();
            services.AddSingleton(TestClock);
            services.AddSingleton<IClock>(sp => sp.GetRequiredService<TestClock>());
        });
    }
}
