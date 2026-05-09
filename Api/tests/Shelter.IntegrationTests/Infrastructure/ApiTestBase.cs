namespace Shelter.IntegrationTests.Infrastructure;

/// <summary>
/// Tier 3 base class. Owns one <see cref="ShelterApiFactory"/> per test class and resets
/// the database before every test.
/// </summary>
[Collection(PostgresCollection.Name)]
public abstract class ApiTestBase : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    protected ShelterApiFactory Factory { get; }
    protected HttpClient Client { get; private set; } = null!;

    protected ApiTestBase(PostgresFixture postgres)
    {
        _postgres = postgres;
        Factory = new ShelterApiFactory(postgres);
    }

    public async Task InitializeAsync()
    {
        await _postgres.ResetAsync();
        Client = Factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        Factory.Dispose();
        return Task.CompletedTask;
    }
}
