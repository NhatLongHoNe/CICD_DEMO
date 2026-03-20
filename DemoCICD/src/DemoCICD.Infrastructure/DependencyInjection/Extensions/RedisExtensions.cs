using DemoCICD.Domain.Abstractions.Repositories;
using DemoCICD.Infrastructure.Caching;
using DemoCICD.Persistence.Repositories;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DemoCICD.Infrastructure.DependencyInjection.Extensions;

public static class RedisExtensions
{
    private const string RedisConfigKey = "Redis:Configuration";

    /// <summary>
    /// Thêm distributed cache (Redis nếu có config, ngược lại memory) và đăng ký CachedPermissionRepository.
    /// </summary>
    public static IServiceCollection AddRedisAndCachedPermissions(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConfig = configuration[RedisConfigKey];
        if (!string.IsNullOrWhiteSpace(redisConfig))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConfig;
                options.InstanceName = configuration["Redis:InstanceName"] ?? "DemoCICD:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddTransient<IPermissionRepository>(sp =>
            new CachedPermissionRepository(
                sp.GetRequiredService<PermissionRepository>(),
                sp.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>()));

        return services;
    }
}
