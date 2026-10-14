using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Sales;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface IPosService
{
    Task<(bool Succeeded, string? Error, int? SaleId)> CheckoutAsync(
        List<CartItemDto> cartItems, CheckoutViewModel model, int branchId, string cashierUserId, CancellationToken cancellationToken = default);

    Task<Sale?> GetSaleAsync(int saleId, CancellationToken cancellationToken = default);
    Task<List<Sale>> GetSalesHistoryAsync(int? branchId = null, CancellationToken cancellationToken = default);
}
