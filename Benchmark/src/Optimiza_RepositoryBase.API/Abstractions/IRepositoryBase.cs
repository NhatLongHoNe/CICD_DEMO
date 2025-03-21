using System.Linq.Expressions;

namespace Optimiza_RepositoryBase.API.Abstractions;

public interface IRepositoryBase<TEntity, in TKey> where TEntity : class
{
    TEntity FindById(TKey id, params Expression<Func<TEntity, object>>[] includeProperties);

    TEntity FindSingle(Expression<Func<TEntity, bool>> predicate, params Expression<Func<TEntity, object>>[] includeProperties);

    IQueryable<TEntity> FindAll(params Expression<Func<TEntity, object>>[] includeProperties);

    IQueryable<TEntity> FindAll(Expression<Func<TEntity, bool>> predicate, params Expression<Func<TEntity, object>>[] includeProperties);

    void Add(TEntity entity);

    void Update(TEntity entity);

    void Remove(TEntity entity);

    void Remove(TKey id);

    void RemoveMultiple(List<TEntity> entities);
}