using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Purchasing;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface IPurchasingService
{
    Task<List<PurchaseOrder>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PurchaseOrder?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error, int? PurchaseOrderId)> CreateDraftAsync(
        CreatePurchaseOrderViewModel model, string createdByUserId, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> AddItemAsync(
        int purchaseOrderId, PurchaseOrderItemViewModel model, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> RemoveItemAsync(int purchaseOrderItemId, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> PlaceOrderAsync(int purchaseOrderId, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> ReceiveItemAsync(
        ReceivePurchaseOrderItemViewModel model, string performedByUserId, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> CancelAsync(int purchaseOrderId, CancellationToken cancellationToken = default);
}
