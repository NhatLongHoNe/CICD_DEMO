using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CleverCore.Infrastructure.Interfaces;
using CleverCore.Infrastructure.SharedKernel;
using Microsoft.EntityFrameworkCore;
using System.Threading; // Additional
using System.Threading.Tasks; // Additional

namespace CleverCore.Data.EF
{
	public interface IRepository<TEntity, in TKey> where TEntity : class
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
        
        // Haven't reached to DB to fetch data so this is not case IO-Bound
        public IQueryable<TEntity> FindAll(params Expression<Func<TEntity, object>>[] includeProperties)
        {
            IQueryable<TEntity> items = _context.Set<TEntity>().AsNoTracking();
            if (includeProperties != null)
                foreach (var includeProperty in includeProperties)
                    items = items.Include(includeProperty);
            return items;
        }

		// Haven't reached to DB to fetch data so this is not case IO-Bound
		public IQueryable<TEntity> FindAll(Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] includeProperties)
        {
            IQueryable<TEntity> items = _context.Set<TEntity>().AsNoTracking();
            if (includeProperties != null)
                foreach (var includeProperty in includeProperties)
                    items = items.Include(includeProperty);
            return items.Where(predicate);
        }

        // IO-Bound
        public async Task<TEntity> FindByIdAsync(TKey id, CancellationToken cancellationToken, params Expression<Func<TEntity, object>>[] includeProperties)
        {
            return await FindAll(includeProperties).SingleOrDefaultAsync(x => x.Id.Equals(id), cancellationToken);
        }

		// IO-Bound
		public async Task<TEntity> FindSingleAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken,
			params Expression<Func<TEntity, object>>[] includeProperties)
        {
            return await FindAll(includeProperties).SingleOrDefaultAsync(predicate, cancellationToken);
        }

		// Working side by side with unitOfWork, this method just change status and is not in case IO-Bound
		public void Add(TEntity entity)
		{
			_context.Add(entity);
		}

		// Working side by side with unitOfWork, this method just change status and is not in case IO-Bound
		public void Update(TEntity entity)
        {
            _context.Set<TEntity>().Update(entity);
        }

		// Working side by side with unitOfWork, this method just change status and is not in case IO-Bound
		public void Remove(TEntity entity)
        {
            _context.Set<TEntity>().Remove(entity);
        }

        public async Task RemoveAsync(TKey id, CancellationToken cancellationToken)
        {
            var entity = await FindByIdAsync(id, cancellationToken);
            Remove(entity);
        }

		// Working side by side with unitOfWork, this method just change status and is not in case IO-Bound
		public void RemoveMultiple(List<TEntity> entities)
        {
            _context.Set<TEntity>().RemoveRange(entities);
        }
	}
}

public static void AddSqlConfiguration(this IServiceCollection services, IConfiguration configuration)
{
    services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("sqlConnection"),
                options => options.MigrationsAssembly("Optimize_RepositoryBase.API")
                .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                ));
}


var blogs = ctx.Blogs
    //.Include(b => b.Posts)
	.AsSplitQuery()
	.Where(b => b.Id.Equals(Id))
    .ToList();

SELECT [b].[BlogId], [b].[OwnerId], [b].[Rating], [b].[Url]
FROM [Blogs] AS [b]
ORDER BY [b].[BlogId]
Where [b].[Id] = Id

SELECT [p].[PostId], [p].[AuthorId], [p].[BlogId], [p].[Content], [p].[Rating], [p].[Title], [b].[BlogId]
FROM [Blogs] AS [b]
INNER JOIN [Posts] AS [p] ON [b].[BlogId] = [p].[BlogId]
ORDER BY [b].[BlogId]














