using System.Text;
using DemoCICD.Application.Abstractions;
using DemoCICD.Infrastructure.Authentication;
using DemoCICD.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DemoCICD.Infrastructure.DependencyInjection.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt options are not configured.");

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IAccessTokenService, JwtTokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        return services;
    }

    /// <summary>
    /// Adds permission-based authorization with custom handler.
    /// Policies: PRODUCT.VIEW, PRODUCT.CREATE, PRODUCT.UPDATE, PRODUCT.DELETE.
    /// 401 if not logged in, 403 if lacking permission.
    /// </summary>
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy(ProductPermissions.View, policy =>
                policy.Requirements.Add(new PermissionRequirement(ProductPermissions.View)));
            options.AddPolicy(ProductPermissions.Create, policy =>
                policy.Requirements.Add(new PermissionRequirement(ProductPermissions.Create)));
            options.AddPolicy(ProductPermissions.Update, policy =>
                policy.Requirements.Add(new PermissionRequirement(ProductPermissions.Update)));
            options.AddPolicy(ProductPermissions.Delete, policy =>
                policy.Requirements.Add(new PermissionRequirement(ProductPermissions.Delete)));
        });

        return services;
    }
}
