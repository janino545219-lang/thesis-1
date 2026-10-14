using FarmaciaSmart.Web.Areas.CustomerPortal.Models.ViewModels;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface ICustomerDashboardService
{
    Task<CustomerDashboardViewModel?> GetDashboardAsync(int customerId, CancellationToken cancellationToken = default);
}
