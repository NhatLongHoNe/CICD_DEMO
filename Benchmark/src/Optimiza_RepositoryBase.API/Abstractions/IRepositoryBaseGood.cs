using System.Linq.Expressions;

namespace Optimiza_RepositoryBase.API.Abstractions;

public interface IRepositoryBaseGood<TEntity, in TKey> where TEntity : class
{
    IQueryable<TEntity> FindAll(params Expression<Func<TEntity, object>>[] includeProperties);

    IQueryable<TEntity> FindAll(Expression<Func<TEntity, bool>> predicate, params Expression<Func<TEntity, object>>[] includeProperties);

    Task<TEntity> FindByIdAsync(TKey id, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includeProperties);

    Task<TEntity> FindSingleAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includeProperties);

    void Add(TEntity entity);

    void Update(TEntity entity);

    void Remove(TEntity entity);

    Task RemoveAsync(TKey id, CancellationToken cancellationToken = default);

    void RemoveMultiple(List<TEntity> entities);
}