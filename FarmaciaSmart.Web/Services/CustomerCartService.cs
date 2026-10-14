using System.Text.Json;
using Microsoft.AspNetCore.Http;
using FarmaciaSmart.Web.Models.ViewModels.Sales;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

/// <summary>
/// Session-scoped shopping cart. A cart's contents are only kept for <see cref="CartLifetime"/>
/// from when the first item was added -- past that, GetCart/AddItem/etc. transparently start a
/// fresh cart. This bounds how long a customer can sit on a stale cart; it does NOT by itself
/// prevent two customers oversubscribing the same limited stock within that window -- that's
/// handled separately by ICustomerOrderService.SubmitOrderAsync checking real availability
/// (current stock minus other customers' still-pending pre-orders) at checkout time.
/// </summary>
public class CustomerCartService(IHttpContextAccessor httpContextAccessor) : ICustomerCartService
{
    private const string CartSessionKey = "Shop_Cart";
    private static readonly TimeSpan CartLifetime = TimeSpan.FromHours(24);

    private ISession Session => httpContextAccessor.HttpContext!.Session;

    public List<CartItemDto> GetCart() => GetEnvelope().Items;

    public void AddItem(int productId, string productName, decimal unitPrice, int quantity)
    {
        var envelope = GetEnvelope();
        var existing = envelope.Items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            envelope.Items.Add(new CartItemDto
            {
                ProductId = productId,
                ProductName = productName,
                UnitPrice = unitPrice,
                Quantity = quantity
            });
        }

        SaveEnvelope(envelope);
    }

    public void RemoveItem(int productId)
    {
        var envelope = GetEnvelope();
        envelope.Items.RemoveAll(i => i.ProductId == productId);
        SaveEnvelope(envelope);
    }

    public void UpdateQuantity(int productId, int quantity)
    {
        var envelope = GetEnvelope();
        var existing = envelope.Items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is null)
        {
            return;
        }

        if (quantity <= 0)
        {
            envelope.Items.Remove(existing);
        }
        else
        {
            existing.Quantity = quantity;
        }

        SaveEnvelope(envelope);
    }

    public void ClearCart() => Session.Remove(CartSessionKey);

    /// <summary>Loads the cart, transparently discarding it (and starting a fresh, empty one) once it's older than CartLifetime.</summary>
    private CartEnvelope GetEnvelope()
    {
        var json = Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(json))
        {
            return new CartEnvelope();
        }

        var envelope = JsonSerializer.Deserialize<CartEnvelope>(json);
        if (envelope is null || DateTime.UtcNow - envelope.CreatedAt > CartLifetime)
        {
            Session.Remove(CartSessionKey);
            return new CartEnvelope();
        }

        return envelope;
    }

    private void SaveEnvelope(CartEnvelope envelope) => Session.SetString(CartSessionKey, JsonSerializer.Serialize(envelope));

    private class CartEnvelope
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<CartItemDto> Items { get; set; } = [];
    }
}
