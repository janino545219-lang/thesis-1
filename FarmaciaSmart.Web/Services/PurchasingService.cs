using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Purchasing;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class PurchasingService(ApplicationDbContext context) : IPurchasingService
{
    public async Task<List<PurchaseOrder>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Branch)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<PurchaseOrder?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Branch)
            .Include(p => p.CreatedByUser)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<(bool Succeeded, string? Error, int? PurchaseOrderId)> CreateDraftAsync(
        CreatePurchaseOrderViewModel model, string createdByUserId, CancellationToken cancellationToken = default)
    {
        if (!await context.Suppliers.AnyAsync(s => s.Id == model.SupplierId && s.IsActive, cancellationToken))
        {
            return (false, "Selected supplier is not valid.", null);
        }

        if (!await context.Branches.AnyAsync(b => b.Id == model.BranchId && b.IsActive, cancellationToken))
        {
            return (false, "Selected branch is not valid.", null);
        }

        var order = new PurchaseOrder
        {
            OrderNumber = string.Empty,
            SupplierId = model.SupplierId,
            BranchId = model.BranchId,
            ExpectedDeliveryDate = model.ExpectedDeliveryDate,
            Notes = model.Notes,
            CreatedByUserId = createdByUserId
        };

        context.PurchaseOrders.Add(order);
        await context.SaveChangesAsync(cancellationToken);

        order.OrderNumber = $"PO-{order.Id:D6}";
        await context.SaveChangesAsync(cancellationToken);

        return (true, null, order.Id);
    }

    public async Task<(bool Succeeded, string? Error)> AddItemAsync(
        int purchaseOrderId, PurchaseOrderItemViewModel model, CancellationToken cancellationToken = default)
    {
        var order = await context.PurchaseOrders.Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == purchaseOrderId, cancellationToken);
        if (order is null)
        {
            return (false, "Purchase order not found.");
        }

        if (order.Status != PurchaseOrderStatus.Draft)
        {
            return (false, "Items can only be added while the purchase order is a draft.");
        }

        if (!await context.Products.AnyAsync(p => p.Id == model.ProductId && p.IsActive, cancellationToken))
        {
            return (false, "Selected product is not valid.");
        }

        context.PurchaseOrderItems.Add(new PurchaseOrderItem
        {
            PurchaseOrderId = purchaseOrderId,
            ProductId = model.ProductId,
            QuantityOrdered = model.QuantityOrdered,
            UnitCost = model.UnitCost
        });

        await context.SaveChangesAsync(cancellationToken);
        await RecalculateTotalAsync(purchaseOrderId, cancellationToken);

        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> RemoveItemAsync(int purchaseOrderItemId, CancellationToken cancellationToken = default)
    {
        var item = await context.PurchaseOrderItems.Include(i => i.PurchaseOrder)
            .FirstOrDefaultAsync(i => i.Id == purchaseOrderItemId, cancellationToken);
        if (item is null)
        {
            return (false, "Purchase order item not found.");
        }

        if (item.PurchaseOrder.Status != PurchaseOrderStatus.Draft)
        {
            return (false, "Items can only be removed while the purchase order is a draft.");
        }

        var purchaseOrderId = item.PurchaseOrderId;
        context.PurchaseOrderItems.Remove(item);
        await context.SaveChangesAsync(cancellationToken);
        await RecalculateTotalAsync(purchaseOrderId, cancellationToken);

        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> PlaceOrderAsync(int purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var order = await context.PurchaseOrders.Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == purchaseOrderId, cancellationToken);
        if (order is null)
        {
            return (false, "Purchase order not found.");
        }

        if (order.Status != PurchaseOrderStatus.Draft)
        {
            return (false, "Only a draft purchase order can be placed.");
        }

        if (order.Items.Count == 0)
        {
            return (false, "Add at least one item before placing the order.");
        }

        order.Status = PurchaseOrderStatus.Ordered;
        order.OrderedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> ReceiveItemAsync(
        ReceivePurchaseOrderItemViewModel model, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var item = await context.PurchaseOrderItems
            .Include(i => i.PurchaseOrder).ThenInclude(o => o.Items)
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == model.PurchaseOrderItemId, cancellationToken);

        if (item is null)
        {
            return (false, "Purchase order item not found.");
        }

        var order = item.PurchaseOrder;
        if (order.Status is not (PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived))
        {
            return (false, "This purchase order is not awaiting receipt.");
        }

        var remaining = item.QuantityOrdered - item.QuantityReceived;
        if (model.Quantity > remaining)
        {
            return (false, $"Only {remaining} unit(s) of {item.Product.Name} remain to be received.");
        }

        if (item.Product.RequiresExpirationTracking && model.ExpirationDate is null)
        {
            return (false, $"{item.Product.Name} requires an expiration date to be recorded for this batch.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var batch = new InventoryBatch
        {
            ProductId = item.ProductId,
            BranchId = order.BranchId,
            BatchNumber = model.BatchNumber,
            ExpirationDate = model.ExpirationDate,
            QuantityReceived = model.Quantity,
            QuantityRemaining = model.Quantity,
            SupplierId = order.SupplierId,
            UnitCost = item.UnitCost
        };
        context.InventoryBatches.Add(batch);
        await context.SaveChangesAsync(cancellationToken);

        var inventory = await context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.BranchId == order.BranchId, cancellationToken);

        if (inventory is null)
        {
            inventory = new Inventory
            {
                ProductId = item.ProductId,
                BranchId = order.BranchId,
                QuantityOnHand = 0
            };
            context.Inventories.Add(inventory);
        }

        inventory.QuantityOnHand += model.Quantity;
        inventory.UpdatedAt = DateTime.UtcNow;

        context.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = item.ProductId,
            BranchId = order.BranchId,
            InventoryBatchId = batch.Id,
            TransactionType = InventoryTransactionType.Purchase,
            Quantity = model.Quantity,
            ReferenceNumber = order.OrderNumber,
            Notes = $"Received against {order.OrderNumber}",
            PerformedByUserId = performedByUserId
        });

        item.QuantityReceived += model.Quantity;

        // `item` is the same tracked instance as its entry in order.Items, so this already sees the updated quantity.
        order.Status = order.Items.All(i => i.QuantityReceived >= i.QuantityOrdered)
            ? PurchaseOrderStatus.Received
            : PurchaseOrderStatus.PartiallyReceived;

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> CancelAsync(int purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var order = await context.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == purchaseOrderId, cancellationToken);
        if (order is null)
        {
            return (false, "Purchase order not found.");
        }

        if (order.Status is not (PurchaseOrderStatus.Draft or PurchaseOrderStatus.Ordered))
        {
            return (false, "Only a draft or ordered purchase order can be cancelled.");
        }

        order.Status = PurchaseOrderStatus.Cancelled;
        await context.SaveChangesAsync(cancellationToken);

        return (true, null);
    }

    private async Task RecalculateTotalAsync(int purchaseOrderId, CancellationToken cancellationToken)
    {
        var order = await context.PurchaseOrders.Include(p => p.Items).FirstAsync(p => p.Id == purchaseOrderId, cancellationToken);
        order.TotalAmount = order.Items.Sum(i => i.UnitCost * i.QuantityOrdered);
        await context.SaveChangesAsync(cancellationToken);
    }
}
