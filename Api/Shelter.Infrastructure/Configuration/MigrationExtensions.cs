using Microsoft.EntityFrameworkCore;
using Shelter.Infrastructure.Persistence;

namespace Shelter.Infrastructure.Configuration;

public static class MigrationExtensions
{
    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
        await db.Database.MigrateAsync();
    }
}
