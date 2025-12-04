using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Bookings;
using Shelter.Domain.Pictures;
using Shelter.Domain.Shelters;
using ShelterModel = Shelter.Domain.Shelters.Shelter;
using Shelter.Domain.Users;

namespace Shelter.Application.Interfaces;

public interface IEntityRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(Guid id);
    Task<List<TEntity>?> ListAsync();
    Task<TEntity> AddAsync(TEntity entity, bool saveChanges = false);
    Task<bool> Update(TEntity entity, bool saveChanges = false);
    Task<bool> Delete(Guid id, bool saveChanges = false);
}

public interface IRepository
{
    public DbSet<ShelterModel> Shelters { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Picture> Pictures { get; set; }
    public DbSet<User> Users { get; set; }
}
