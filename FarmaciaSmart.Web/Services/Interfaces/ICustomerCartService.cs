using FarmaciaSmart.Web.Models.ViewModels.Sales;

namespace FarmaciaSmart.Web.Services.Interfaces;

/// <summary>
/// Session-scoped shopping cart for the Customer Portal's Shop feature. Deliberately separate
/// from ICartService (POS's staff-side cart) -- different session key, different domain, even
/// though the shape is identical -- so a staff member and a customer never share cart state.
/// </summary>
public interface ICustomerCartService
{
    List<CartItemDto> GetCart();
    void AddItem(int productId, string productName, decimal unitPrice, int quantity);
    void RemoveItem(int productId);
    void UpdateQuantity(int productId, int quantity);
    void ClearCart();
}
