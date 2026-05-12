using App.Common;
using App.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Shelter.Infrastructure;

namespace Shelter.IntegrationTests.Infrastructure;

/// <summary>
/// Tier 2 base class: handler ↔ real Postgres ↔ real EF, no HTTP. Builds the same DI graph
/// that <c>Program.cs</c> would, then swaps <see cref="IFileStorage"/> for an in-memory fake
/// and <see cref="IClock"/> for a settable one. Each test gets a fresh DI scope and a clean
/// database (Respawn truncates user-data tables in <see cref="InitializeAsync"/>).
/// </summary>
[Collection(PostgresCollection.Name)]
public abstract class HandlerTestBase : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private readonly ServiceProvider _root;
    private IServiceScope _scope = null!;

    protected HandlerTestBase(PostgresFixture postgres)
    {
        _postgres = postgres;

        var configuration = BuildConfiguration(postgres.ConnectionString);
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        services.AddLogging();

        services.AddInfrastructure(configuration);

        services.RemoveAll<IFileStorage>();
        services.AddSingleton<IFileStorage, InMemoryFileStorage>();

        services.RemoveAll<IClock>();
        services.AddSingleton<TestClock>();
        services.AddSingleton<IClock>(sp => sp.GetRequiredService<TestClock>());

        _root = services.BuildServiceProvider();
    }

    /// <summary>Service scope for the current test. Resolved services share one DbContext.</summary>
    protected IServiceProvider Services => _scope.ServiceProvider;

    protected IShelterDbContext Db => Services.GetRequiredService<IShelterDbContext>();

    protected TestClock Clock => Services.GetRequiredService<TestClock>();

    public async Task InitializeAsync()
    {
        await _postgres.ResetAsync();
        _scope = _root.CreateScope();
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    private static IConfiguration BuildConfiguration(string connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
                ["Jwt:Issuer"] = "https://test.local",
                ["Jwt:Audience"] = "shelter-api-tests",
                ["Jwt:Secret"] = "test-only-secret-padded-to-meet-hs256-min-length-1234567890",
                ["Jwt:AccessTokenMinutes"] = "60",
                ["BlobStorage:ConnectionString"] = "UseDevelopmentStorage=true",
                ["BlobStorage:ContainerName"] = "test-images",
                ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
            })
            .Build();
}
