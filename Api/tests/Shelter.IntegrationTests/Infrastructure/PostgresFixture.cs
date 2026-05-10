using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Images;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Shelter.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Shelter.IntegrationTests.Infrastructure;

/// <summary>
/// Spins up a single Postgres+PostGIS container for the whole test session, applies the EF
/// migrations once, and exposes a Respawn checkpoint that resets user-data tables between
/// tests. Identity roles are excluded from the reset so the seed survives across tests.
///
/// The image is built from <c>Api/docker/postgres/Dockerfile</c> (official <c>postgres:18</c>
/// + <c>postgresql-18-postgis-3</c>) so this fixture and <c>docker compose</c> share a single
/// recipe; subsequent test runs reuse the cached layer.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly IFutureDockerImage _image = new ImageFromDockerfileBuilder()
        .WithName("shelter-postgis:test")
        .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), "docker/postgres")
        .WithDockerfile("Dockerfile")
        .WithDeleteIfExists(false) // reuse the cached image across runs
        .Build();

    private PostgreSqlContainer _container = null!;
    private Respawner _respawner = null!;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _image.CreateAsync();

        _container = new PostgreSqlBuilder()
            .WithImage(_image)
            .WithDatabase("shelter")
            .WithUsername("shelter")
            .WithPassword("shelter")
            .Build();

        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<ShelterDbContext>()
            .UseNpgsql(ConnectionString, o => o.UseNetTopologySuite())
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
