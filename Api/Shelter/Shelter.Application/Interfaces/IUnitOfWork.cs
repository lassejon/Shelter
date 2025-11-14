namespace Shelter.Application.Interfaces;

public interface IUnitOfWork
{
    Task CommitChangesAsync();
}