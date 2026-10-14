using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Data.Seed;

public static class IdentitySeeder
{
    private static readonly (string Code, string Name, string Module)[] BasePermissions =
    [
        ("branches.view", "View Branches", "Branch"),
        ("branches.manage", "Manage Branches", "Branch"),
        ("users.view", "View Users", "Security"),
        ("users.manage", "Manage Users", "Security"),
    ];

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var context = services.GetRequiredService<ApplicationDbContext>();

        foreach (var roleName in ApplicationRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            }
        }

        await SeedPermissionsAsync(context);
        await SeedDevAdminAsync(userManager, configuration);
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext context)
    {
        foreach (var (code, name, module) in BasePermissions)
        {
            if (!await context.Permissions.AnyAsync(p => p.Code == code))
            {
                context.Permissions.Add(new Permission { Code = code, Name = name, Module = module });
            }
        }

        await context.SaveChangesAsync();

        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == ApplicationRoles.Administrator);
        if (adminRole is null)
        {
            return;
        }

        var allPermissionIds = await context.Permissions.Select(p => p.Id).ToListAsync();
        var alreadyLinked = await context.RolePermissions
            .Where(rp => rp.RoleId == adminRole.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        foreach (var permissionId in allPermissionIds.Except(alreadyLinked))
        {
            context.RolePermissions.Add(new RolePermission { RoleId = adminRole.Id, PermissionId = permissionId });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedDevAdminAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        var section = configuration.GetSection("SeedAdmin");
        var email = section["Email"];
        var password = section["Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "System",
            LastName = "Administrator",
            IsActive = true
        };

        var result = await userManager.CreateAsync(admin, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, ApplicationRoles.Administrator);
        }
    }
}
