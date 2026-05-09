using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Shelter.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Shelter.IntegrationTests.Infrastructure;

/// <summary>
/// Spins up a single Postgres container for the whole test session, applies the EF migrations
/// once, and exposes a Respawn checkpoint that resets user-data tables between tests. Identity
/// roles are excluded from the reset so the seed survives across tests.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:18-alpine")
        .WithDatabase("shelter")
        .WithUsername("shelter")
        .WithPassword("shelter")
        .Build();

    private Respawner _respawner = null!;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<ShelterDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using (var db = new ShelterDbContext(options))
        {
            await db.Database.MigrateAsync();
        }

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore =
            [
                new("__EFMigrationsHistory"),
                new("AspNetRoles"),
            ],
        });
    }

    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
