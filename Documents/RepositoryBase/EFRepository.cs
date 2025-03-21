using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CleverCore.Infrastructure.Interfaces;
using CleverCore.Infrastructure.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace CleverCore.Data.EF
{
	
    public interface IRepository<TEntity, in TKey> where TEntity : class
    {
        IQueryable<TEntity> FindAll(params Expression<Func<TEntity, object>>[] includeProperties);

        IQueryable<TEntity> FindAll(Expression<Func<TEntity, bool>> predicate, params Expression<Func<TEntity, object>>[] includeProperties);
		
		TEntity FindById(TKey id, params Expression<Func<TEntity, object>>[] includeProperties);

        TEntity FindSingle(Expression<Func<TEntity, bool>> predicate, params Expression<Func<TEntity, object>>[] includeProperties);


        void Add(TEntity entity);

        void Update(TEntity entity);

        void Remove(TEntity entity);

        void Remove(TKey id);

        void RemoveMultiple(List<TEntity> entities);
    }


    public class EfRepository<TEntity, TKey> : IRepository<TEntity, TKey>, IDisposable
        where TEntity : DomainEntity<TKey>
    {
        private readonly AppDbContext _context;

        public EfRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        public void Add(TEntity entity)
        {
            _context.Add(entity);
        }

        public IQueryable<TEntity> FindAll(params Expression<Func<TEntity, object>>[] includeProperties)
        {
            IQueryable<TEntity> items = _context.Set<TEntity>();
            if (includeProperties != null)
                foreach (var includeProperty in includeProperties)
                    items = items.Include(includeProperty);
            return items;
        }

        public IQueryable<TEntity> FindAll(Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] includeProperties)
        {
            IQueryable<TEntity> items = _context.Set<TEntity>();
            if (includeProperties != null)
                foreach (var includeProperty in includeProperties)
                    items = items.Include(includeProperty);
            return items.Where(predicate);
        }

        public TEntity FindById(TKey id, params Expression<Func<TEntity, object>>[] includeProperties)
        {
            return FindAll(includeProperties).SingleOrDefault(x => x.Id.Equals(id));
        }

        public TEntity FindSingle(Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] includeProperties)
        {
            return FindAll(includeProperties).SingleOrDefault(predicate);
        }

        public void Remove(TEntity entity)
        {
            _context.Set<TEntity>().Remove(entity);
        }

        public void Remove(TKey id)
        {
            var entity = FindById(id); // =>> Reach out database to get data
            Remove(entity);
        }

        public void RemoveMultiple(List<TEntity> entities)
        {
            _context.Set<TEntity>().RemoveRange(entities);
        }

        public void Update(TEntity entity)
        {
            _context.Set<TEntity>().Update(entity);
        }
    }
}


public void ConfigureServices(IServiceCollection services)
{
	services.AddDbContext<AppDbContext>(options =>
		   options.UseSqlServer(Configuration.GetConnectionString("AppDbConnection"),
			   b => b.MigrationsAssembly("TeduCore.Data.EF")));
}





var blogs = ctx.Blogs
    //.Include(b => b.Posts)
	.Where(b => b.Id.Equals(Id))
    .ToList();

SELECT [b].[Id], [b].[Name], [b].[HugeColumn], [p].[Id], [p].[BlogId], [p].[Title]
FROM [Blogs] AS [b]
LEFT JOIN [Posts] AS [p] ON [b].[Id] = [p].[BlogId]
Where [b].[Id] = Id
ORDER BY [b].[Id]


// Khong Include => AsNoTracking()
// Co Include => SplitQuery()









