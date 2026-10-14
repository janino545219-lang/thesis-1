using FarmaciaSmart.Web.Models.ViewModels.Sales;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface ICartService
{
    List<CartItemDto> GetCart();
    void AddItem(int productId, string productName, decimal unitPrice, int quantity);
    void RemoveItem(int productId);
    void ClearCart();

    int? GetOperatingBranchId();
    void SetOperatingBranchId(int branchId);
}
