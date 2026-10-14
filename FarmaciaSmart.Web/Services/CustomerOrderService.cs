using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Sales;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class CustomerOrderService(ApplicationDbContext context) : ICustomerOrderService
{
    public async Task<(bool Succeeded, string? Error, int? OrderId)> SubmitOrderAsync(
        int customerId, int branchId, List<CartItemDto> cartItems, string? notes, CancellationToken cancellationToken = default)
    {
        if (cartItems.Count == 0)
        {
            return (false, "Your cart is empty.", null);
        }

        if (!await context.Branches.AnyAsync(b => b.Id == branchId && b.IsActive, cancellationToken))
        {
            return (false, "Selected pickup branch is not valid.", null);
        }

        var order = new CustomerOrder
        {
            OrderNumber = string.Empty,
            CustomerId = customerId,
            BranchId = branchId,
            Notes = notes
        };

        var total = 0m;

        foreach (var cartItem in cartItems)
        {
            var product = await context.Products.FirstOrDefaultAsync(p => p.Id == cartItem.ProductId && p.IsActive, cancellationToken);
            if (product is null)
            {
                return (false, $"{cartItem.ProductName} is no longer available.", null);
            }

            // Prevent two customers jointly pre-ordering more than the branch actually has: cap
            // against current stock minus whatever other customers' still-pending pre-orders
            // already claim for this product at this branch. This is the real fix for the
            // "two customers each pre-order more than exists" scenario -- cart expiry alone
            // doesn't touch it, since both could check out well within the cart's lifetime.
            var quantityOnHand = await context.Inventories
                .Where(i => i.ProductId == product.Id && i.BranchId == branchId)
                .Select(i => (int?)i.QuantityOnHand)
                .FirstOrDefaultAsync(cancellationToken) ?? 0;

            var alreadyPending = await context.CustomerOrderItems
                .Where(i => i.ProductId == product.Id &&
                            i.CustomerOrder.BranchId == branchId &&
                            i.CustomerOrder.Status == CustomerOrderStatus.Pending)
                .SumAsync(i => (int?)i.Quantity, cancellationToken) ?? 0;

            var available = quantityOnHand - alreadyPending;
            if (cartItem.Quantity > available)
            {
                var error = available <= 0
                    ? $"{product.Name} is fully claimed by other pending pre-orders right now. Please check back later."
                    : $"Only {available} unit(s) of {product.Name} are available for pre-order right now -- the rest is already claimed by other pending pre-orders.";
                return (false, error, null);
            }

            order.Items.Add(new CustomerOrderItem
            {
                ProductId = product.Id,
                Quantity = cartItem.Quantity,
                UnitPriceAtOrderTime = product.Price
            });

            total += product.Price * cartItem.Quantity;
        }

        order.TotalAmount = total;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        context.CustomerOrders.Add(order);
        await context.SaveChangesAsync(cancellationToken);

        order.OrderNumber = $"CO-{order.Id:D6}";
        await context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return (true, null, order.Id);
    }

    public async Task<CustomerOrder?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.CustomerOrders
            .Include(o => o.Customer).ThenInclude(c => c.User)
            .Include(o => o.Branch)
            .Include(o => o.FulfilledByUser)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<List<CustomerOrder>> GetByCustomerAsync(int customerId, CancellationToken cancellationToken = default) =>
        await context.CustomerOrders
            .Include(o => o.Branch)
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<List<CustomerOrder>> GetPendingAsync(CancellationToken cancellationToken = default) =>
        await context.CustomerOrders
            .Include(o => o.Customer).ThenInclude(c => c.User)
            .Include(o => o.Branch)
            .Include(o => o.Items)
            .Where(o => o.Status == CustomerOrderStatus.Pending)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<(bool Succeeded, string? Error)> FulfillAsync(int orderId, string fulfilledByUserId, CancellationToken cancellationToken = default)
    {
        var order = await context.CustomerOrders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null)
        {
            return (false, "Order not found.");
        }

        if (order.Status != CustomerOrderStatus.Pending)
        {
            return (false, "Only a pending order can be fulfilled.");
        }

        order.Status = CustomerOrderStatus.Fulfilled;
        order.FulfilledByUserId = fulfilledByUserId;
        order.FulfilledAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> CancelAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await context.CustomerOrders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null)
        {
            return (false, "Order not found.");
        }

        if (order.Status != CustomerOrderStatus.Pending)
        {
            return (false, "Only a pending order can be cancelled.");
        }

        order.Status = CustomerOrderStatus.Cancelled;
        await context.SaveChangesAsync(cancellationToken);

        return (true, null);
    }
}
