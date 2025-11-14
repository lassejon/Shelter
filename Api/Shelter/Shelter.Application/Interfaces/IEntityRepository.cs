namespace Shelter.Application.Interfaces;

public interface IEntityRepository<TEntity>
{
    Task<TEntity?> GetByIdAsync(Guid id);
    Task<List<TEntity>?> ListAsync();
    Task<TEntity> AddAsync(TEntity entity, bool saveChanges = false);
    Task<bool> Update(TEntity entity, bool saveChanges = false);
    Task<bool> Delete(Guid id, bool saveChanges = false);
}