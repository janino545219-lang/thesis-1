using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Sales;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface ICustomerOrderService
{
    Task<(bool Succeeded, string? Error, int? OrderId)> SubmitOrderAsync(
        int customerId, int branchId, List<CartItemDto> cartItems, string? notes, CancellationToken cancellationToken = default);

    Task<CustomerOrder?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<CustomerOrder>> GetByCustomerAsync(int customerId, CancellationToken cancellationToken = default);
    Task<List<CustomerOrder>> GetPendingAsync(CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> FulfillAsync(int orderId, string fulfilledByUserId, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> CancelAsync(int orderId, CancellationToken cancellationToken = default);
}
