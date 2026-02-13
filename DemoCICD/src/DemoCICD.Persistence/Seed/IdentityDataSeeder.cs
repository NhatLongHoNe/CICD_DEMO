using DemoCICD.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DemoCICD.Persistence.Seed;

/// <summary>
/// Seed Users + UserRoles khi DB đã có RBAC data (Actions, Functions, Roles, Permissions).
/// Chạy 1 lần khi ứng dụng start, dùng UserManager để hash password đúng format Identity.
/// Password mặc định: Password123!
/// </summary>
public sealed class IdentityDataSeeder : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<IdentityDataSeeder> _logger;

    public IdentityDataSeeder(IServiceProvider services, ILogger<IdentityDataSeeder> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        if (await userManager.FindByIdAsync(IdentitySeedData.UserSuperAdminId.ToString()) is not null)
        {
            _logger.LogInformation("Identity seed data already exists. Skipping.");
            return;
        }

        _logger.LogInformation("Seeding identity users...");

        const string defaultPassword = "Password123!";

        var users = new (AppUser User, string[] Roles)[]
        {
            (CreateUser(IdentitySeedData.UserSuperAdminId, "superadmin", "Super", "Admin", true), new[] { "SuperAdmin" }),
            (CreateUser(IdentitySeedData.UserManager1Id, "manager1", "Nguyen Van", "Manager", false), new[] { "Manager" }),
            (CreateUser(IdentitySeedData.UserManager2Id, "manager2", "Tran Thi", "Manager", false), new[] { "Manager" }),
            (CreateUser(IdentitySeedData.UserStaff1Id, "staff1", "Le Van", "Staff", false), new[] { "Staff" }),
            (CreateUser(IdentitySeedData.UserStaff2Id, "staff2", "Pham Thi", "Staff", false), new[] { "Staff" }),
            (CreateUser(IdentitySeedData.UserStaff3Id, "staff3", "Hoang Van", "Staff", false), new[] { "Staff" }),
            (CreateUser(IdentitySeedData.UserMultiRoleId, "multirole", "Multi", "Role", false), new[] { "Manager", "Staff" }),
            (CreateUser(IdentitySeedData.UserLimitedId, "limited", "Limited", "User", false), new[] { "Staff" }),
            (CreateUser(IdentitySeedData.UserNoApproveId, "noapprove", "No Approve", "Manager", false), new[] { "Manager" })
        };

        foreach (var (user, roles) in users)
        {
            var existing = await userManager.FindByIdAsync(user.Id.ToString());
            if (existing is not null)
                continue;

            var result = await userManager.CreateAsync(user, defaultPassword);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to create user {UserName}: {Errors}",
                    user.UserName,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                continue;
            }

            await userManager.AddToRolesAsync(user, roles);
            _logger.LogInformation("Created user {UserName} with roles {Roles}", user.UserName, string.Join(", ", roles));
        }

        _logger.LogInformation("Identity seed completed.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static AppUser CreateUser(Guid id, string userName, string firstName, string lastName, bool isDirector)
    {
        return new AppUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = $"{userName}@democicd.local",
            NormalizedEmail = $"{userName}@democicd.local".ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            FullName = $"{firstName} {lastName}",
            PositionId = IdentitySeedData.PositionDefaultId,
            IsDirector = isDirector,
            IsHeadOfDepartment = isDirector,
            IsReceipient = 0,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
    }
}
