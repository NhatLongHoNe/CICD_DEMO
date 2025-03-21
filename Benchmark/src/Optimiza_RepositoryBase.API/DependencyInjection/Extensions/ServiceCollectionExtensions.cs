using Microsoft.EntityFrameworkCore;
using Optimiza_RepositoryBase.API.Abstractions;
using Optimiza_RepositoryBase.API.Applications.Implements;
using Optimiza_RepositoryBase.API.Applications.Interfaces;
using Optimiza_RepositoryBase.API.Infrastructure;
using Optimiza_RepositoryBase.API.Repositories;

namespace Optimiza_RepositoryBase.API.DependencyInjection.Extensions;

public static class ServiceCollectionExtensions
{

    public static void AddSqlConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("sqlConnection"),
                    options => options.MigrationsAssembly("Optimiza_RepositoryBase.API")
                    .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                    ));
    }
    public static void AddServiceConfiguration(this IServiceCollection services)
    {
        services.AddTransient(typeof(IUnitOfWork), typeof(EFUnitOfWork));
        services.AddTransient(typeof(IRepositoryBase<,>), typeof(RepositoryBase<,>));
        services.AddTransient(typeof(IRepositoryBaseGood<,>), typeof(RepositoryBaseGood<,>));

        services.AddTransient<IStudentService, StudentService>();
        services.AddTransient<IStudentServiceGood, StudentServiceGood>();
    }

}
