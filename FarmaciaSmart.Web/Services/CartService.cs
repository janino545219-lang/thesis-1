using System.Text.Json;
using Microsoft.AspNetCore.Http;
using FarmaciaSmart.Web.Models.ViewModels.Sales;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class CartService(IHttpContextAccessor httpContextAccessor) : ICartService
{
    private const string CartSessionKey = "POS_Cart";
    private const string BranchSessionKey = "POS_BranchId";

    private ISession Session => httpContextAccessor.HttpContext!.Session;

    public List<CartItemDto> GetCart()
    {
        var json = Session.GetString(CartSessionKey);
        return string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize<List<CartItemDto>>(json) ?? [];
    }

    public void AddItem(int productId, string productName, decimal unitPrice, int quantity)
    {
        var items = GetCart();
        var existing = items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            items.Add(new CartItemDto
            {
                ProductId = productId,
                ProductName = productName,
                UnitPrice = unitPrice,
                Quantity = quantity
            });
        }

        SaveCart(items);
    }

    public void RemoveItem(int productId)
    {
        var items = GetCart();
        items.RemoveAll(i => i.ProductId == productId);
        SaveCart(items);
    }

    public void ClearCart() => Session.Remove(CartSessionKey);

    public int? GetOperatingBranchId() => Session.GetInt32(BranchSessionKey);

    public void SetOperatingBranchId(int branchId) => Session.SetInt32(BranchSessionKey, branchId);

    private void SaveCart(List<CartItemDto> items) => Session.SetString(CartSessionKey, JsonSerializer.Serialize(items));
}
