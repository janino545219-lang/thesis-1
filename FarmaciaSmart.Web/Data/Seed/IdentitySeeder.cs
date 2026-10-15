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
        await SeedDevAdminAsync(userManager, context, configuration);
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

        foreach (var rName in new[] { ApplicationRoles.SuperAdmin, ApplicationRoles.Administrator })
        {
            var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == rName);
            if (role is not null)
            {
                var allPermissionIds = await context.Permissions.Select(p => p.Id).ToListAsync();
                var alreadyLinked = await context.RolePermissions
                    .Where(rp => rp.RoleId == role.Id)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync();

                foreach (var permissionId in allPermissionIds.Except(alreadyLinked))
                {
                    context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
                }
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedDevAdminAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IConfiguration configuration)
    {
        var section = configuration.GetSection("SeedAdmin");
        var email = section["Email"];
        var password = section["Password"];

        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
        {
            if (await userManager.FindByEmailAsync(email) is null)
            {
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

        // Seed dedicated Super Admin user based on ACM Use Case diagram
        const string superAdminEmail = "superadmin@farmaciasanantonio.ph";
        if (await userManager.FindByEmailAsync(superAdminEmail) is null)
        {
            var superAdmin = new ApplicationUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                EmailConfirmed = true,
                FirstName = "Super",
                LastName = "Admin",
                IsActive = true
            };

            var saResult = await userManager.CreateAsync(superAdmin, "SuperAdmin123!");
            if (saResult.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdmin, ApplicationRoles.SuperAdmin);
                await userManager.AddToRoleAsync(superAdmin, ApplicationRoles.Administrator);
            }
        }
        else
        {
            var existingSuperAdmin = await userManager.FindByEmailAsync(superAdminEmail);
            if (existingSuperAdmin != null)
            {
                if (!await userManager.IsInRoleAsync(existingSuperAdmin, ApplicationRoles.SuperAdmin))
                {
                    await userManager.AddToRoleAsync(existingSuperAdmin, ApplicationRoles.SuperAdmin);
                }
                if (!await userManager.IsInRoleAsync(existingSuperAdmin, ApplicationRoles.Administrator))
                {
                    await userManager.AddToRoleAsync(existingSuperAdmin, ApplicationRoles.Administrator);
                }
            }
        }

        // Also ensure Jerimie Anino (Admin/Owner/Super Admin) is seeded
        const string jerimieEmail = "jerimieanino@gmail.com";
        var existingJerimie = await userManager.FindByEmailAsync(jerimieEmail);
        if (existingJerimie is null)
        {
            var jerimie = new ApplicationUser
            {
                UserName = jerimieEmail,
                Email = jerimieEmail,
                EmailConfirmed = true,
                FirstName = "Jerimie",
                LastName = "Anino",
                IsActive = true
            };

            var jResult = await userManager.CreateAsync(jerimie, "Admin123!");
            if (jResult.Succeeded)
            {
                await userManager.AddToRoleAsync(jerimie, ApplicationRoles.Administrator);
                await userManager.AddToRoleAsync(jerimie, ApplicationRoles.SuperAdmin);
            }
        }
        else
        {
            if (!await userManager.IsInRoleAsync(existingJerimie, ApplicationRoles.SuperAdmin))
            {
                await userManager.AddToRoleAsync(existingJerimie, ApplicationRoles.SuperAdmin);
            }
            if (!await userManager.IsInRoleAsync(existingJerimie, ApplicationRoles.Administrator))
            {
                await userManager.AddToRoleAsync(existingJerimie, ApplicationRoles.Administrator);
            }
        }

        // Also ensure Branch Manager is seeded for testing
        const string managerEmail = "manager.main@farmaciasanantonio.ph";
        if (await userManager.FindByEmailAsync(managerEmail) is null)
        {
            var mainBranch = await context.Branches.FirstOrDefaultAsync(b => b.Name.Contains("Main"));
            var manager = new ApplicationUser
            {
                UserName = managerEmail,
                Email = managerEmail,
                EmailConfirmed = true,
                FirstName = "Branch",
                LastName = "Manager",
                BranchId = mainBranch?.Id,
                IsActive = true
            };

            var mResult = await userManager.CreateAsync(manager, "Manager123!");
            if (mResult.Succeeded)
            {
                await userManager.AddToRoleAsync(manager, ApplicationRoles.BranchManager);
            }
        }

        // Seed Pharmacist user based on ACM Use Case diagram (Figure 5)
        const string pharmacistEmail = "pharmacist@farmaciasanantonio.ph";
        var existingPharmacist = await userManager.FindByEmailAsync(pharmacistEmail);
        if (existingPharmacist is null)
        {
            var mainBranch = await context.Branches.FirstOrDefaultAsync(b => b.Name.Contains("Main"));
            var pharmacist = new ApplicationUser
            {
                UserName = pharmacistEmail,
                Email = pharmacistEmail,
                EmailConfirmed = true,
                FirstName = "Licensed",
                LastName = "Pharmacist",
                BranchId = mainBranch?.Id,
                IsActive = true
            };

            var pResult = await userManager.CreateAsync(pharmacist, "Pharmacist123!");
            if (pResult.Succeeded)
            {
                await userManager.AddToRoleAsync(pharmacist, ApplicationRoles.Pharmacist);
            }
        }
        else
        {
            if (!await userManager.IsInRoleAsync(existingPharmacist, ApplicationRoles.Pharmacist))
            {
                await userManager.AddToRoleAsync(existingPharmacist, ApplicationRoles.Pharmacist);
            }
        }
    }
}
