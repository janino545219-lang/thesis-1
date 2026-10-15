using FarmaciaSmart.Web.Models.ViewModels.Home;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface IStaffHomeService
{
    Task<StaffHomeViewModel> GetSummaryAsync(
        bool isAdministrator, bool isCashier, bool isInventoryStaff, bool isPharmacist, bool isHrPayroll,
        int? branchId, bool isBranchManager = false, bool isSuperAdmin = false, CancellationToken cancellationToken = default);
}
