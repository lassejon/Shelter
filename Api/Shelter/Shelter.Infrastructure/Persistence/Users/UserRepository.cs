using Microsoft.EntityFrameworkCore;
using Shelter.Application.Interfaces;
using Shelter.Domain.Users;

namespace Shelter.Infrastructure.Persistence.Users;

internal class UserRepository(ShelterDbContext dbContext) : IEntityRepository<User>
{
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await dbContext.Users.FindAsync(id);
    }

    public async Task<List<User>?> ListAsync()
    {
        return await dbContext.Users.ToListAsync();
    }

    public async Task<User> AddAsync(User entity, bool saveChanges = false)
    {
        var entityEntry = await dbContext.Users.AddAsync(entity);
        
        if (saveChanges)
        {
            await dbContext.SaveChangesAsync();
        }
        
        return entityEntry.Entity;
    }

    public async Task<bool> Update(User entity, bool saveChanges = false)
    {
        var entityEntry = dbContext.Users.Update(entity);
        
        if (saveChanges)
        {
            await dbContext.SaveChangesAsync();
        }
        
        return entityEntry.State == EntityState.Modified;
    }

    public async Task<bool> Delete(Guid id, bool saveChanges = false)
    {
        var entityEntry = dbContext.Users.Remove(new User { Id = id });
        
        if (saveChanges)
        {
            await dbContext.SaveChangesAsync();
        }
        
        return entityEntry.State == EntityState.Deleted;
    }
}