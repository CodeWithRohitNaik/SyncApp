using System.Linq.Expressions;

namespace ThrustSync.Core.Repositories;

/// <summary>
/// Generic repository interface for data access
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>Gets all entities</summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>Gets entity by id</summary>
    Task<T?> GetByIdAsync(int id);

    /// <summary>Gets entities matching a predicate</summary>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

    /// <summary>Gets the first entity matching a predicate</summary>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

    /// <summary>Adds an entity</summary>
    Task AddAsync(T entity);

    /// <summary>Adds multiple entities</summary>
    Task AddRangeAsync(IEnumerable<T> entities);

    /// <summary>Updates an entity</summary>
    void Update(T entity);

    /// <summary>Updates multiple entities</summary>
    void UpdateRange(IEnumerable<T> entities);

    /// <summary>Removes an entity</summary>
    void Remove(T entity);

    /// <summary>Removes multiple entities</summary>
    void RemoveRange(IEnumerable<T> entities);

    /// <summary>Saves changes to the database</summary>
    Task<int> SaveChangesAsync();

    /// <summary>Gets count of entities matching a predicate</summary>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
}
