using Microsoft.AspNetCore.Identity;

namespace FarmaciaSmart.Web.Models.Entities;

public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
