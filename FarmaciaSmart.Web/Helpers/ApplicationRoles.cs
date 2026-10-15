namespace FarmaciaSmart.Web.Helpers;

public static class ApplicationRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Administrator = "Administrator";
    public const string BranchManager = "BranchManager";
    public const string Pharmacist = "Pharmacist";
    public const string Cashier = "Cashier";
    public const string InventoryStaff = "InventoryStaff";
    public const string HrPayroll = "HrPayroll";
    public const string Customer = "Customer";

    public const string AdminOrSuper = $"{SuperAdmin},{Administrator}";

    public static readonly string[] StaffRoles =
    [
        SuperAdmin, Administrator, BranchManager, Pharmacist, Cashier, InventoryStaff, HrPayroll
    ];

    public static readonly string[] All = [.. StaffRoles, Customer];
}
