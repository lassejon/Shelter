using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shelter.Application.Interfaces;
using Shelter.Domain.Users;

namespace Shelter.Infrastructure.Persistence;

public class ShelterDbContext(DbContextOptions options) : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options), IUnitOfWork
{
    public override DbSet<User> Users { get; set; } = null!;

    public async Task CommitChangesAsync()
    {
        await base.SaveChangesAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.EnableSensitiveDataLogging();
    }
}