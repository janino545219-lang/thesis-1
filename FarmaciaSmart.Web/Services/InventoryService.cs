using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels;
using FarmaciaSmart.Web.Models.ViewModels.Inventories;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class InventoryService(ApplicationDbContext context) : IInventoryService
{
    public async Task<List<StockRowViewModel>> GetStockAsync(CancellationToken cancellationToken = default)
    {
        var inventories = await context.Inventories
            .Include(i => i.Product)
            .Include(i => i.Branch)
            .OrderBy(i => i.Branch.Name).ThenBy(i => i.Product.Name)
            .ToListAsync(cancellationToken);

        var nearestExpirations = await context.InventoryBatches
            .Where(b => b.QuantityRemaining > 0 && b.ExpirationDate != null)
            .GroupBy(b => new { b.ProductId, b.BranchId })
            .Select(g => new { g.Key.ProductId, g.Key.BranchId, NearestExpiration = g.Min(b => b.ExpirationDate) })
            .ToListAsync(cancellationToken);

        var expirationLookup = nearestExpirations.ToDictionary(
            x => (x.ProductId, x.BranchId),
            x => x.NearestExpiration);

        return inventories.Select(i => new StockRowViewModel
        {
            Inventory = i,
            NearestExpiration = expirationLookup.TryGetValue((i.ProductId, i.BranchId), out var exp) ? exp : null
        }).ToList();
    }

    public async Task<PagedResult<InventoryTransaction>> GetRecentTransactionsAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        var query = context.InventoryTransactions
            .Include(t => t.Product)
            .Include(t => t.Branch)
            .Include(t => t.PerformedByUser)
            .OrderByDescending(t => t.Timestamp);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<InventoryTransaction> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
    }

    public async Task<(bool Succeeded, string? Error)> StockInAsync(StockInViewModel model, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == model.ProductId && p.IsActive, cancellationToken);
        if (product is null)
        {
            return (false, "Selected product is not valid.");
        }

        if (!await context.Branches.AnyAsync(b => b.Id == model.BranchId && b.IsActive, cancellationToken))
        {
            return (false, "Selected branch is not valid.");
        }

        if (product.RequiresExpirationTracking && model.ExpirationDate is null)
        {
            return (false, $"{product.Name} requires an expiration date to be recorded for this batch.");
        }

        if (model.SupplierId.HasValue &&
            !await context.Suppliers.AnyAsync(s => s.Id == model.SupplierId && s.IsActive, cancellationToken))
        {
            return (false, "Selected supplier is not valid.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var batch = new InventoryBatch
        {
            ProductId = model.ProductId,
            BranchId = model.BranchId,
            BatchNumber = model.BatchNumber,
            ExpirationDate = model.ExpirationDate,
            QuantityReceived = model.Quantity,
            QuantityRemaining = model.Quantity,
            SupplierId = model.SupplierId,
            UnitCost = model.UnitCost
        };
        context.InventoryBatches.Add(batch);
        await context.SaveChangesAsync(cancellationToken);

        var inventory = await context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == model.ProductId && i.BranchId == model.BranchId, cancellationToken);

        if (inventory is null)
        {
            inventory = new Inventory
            {
                ProductId = model.ProductId,
                BranchId = model.BranchId,
                QuantityOnHand = 0
            };
            context.Inventories.Add(inventory);
        }

        inventory.QuantityOnHand += model.Quantity;
        inventory.UpdatedAt = DateTime.UtcNow;

        context.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = model.ProductId,
            BranchId = model.BranchId,
            InventoryBatchId = batch.Id,
            TransactionType = InventoryTransactionType.Purchase,
            Quantity = model.Quantity,
            Notes = model.Notes,
            PerformedByUserId = performedByUserId
        });

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> AdjustStockAsync(StockAdjustmentViewModel model, string performedByUserId, CancellationToken cancellationToken = default)
    {
        if (model.AdjustmentQuantity == 0)
        {
            return (false, "Adjustment quantity cannot be zero.");
        }

        if (!await context.Products.AnyAsync(p => p.Id == model.ProductId && p.IsActive, cancellationToken))
        {
            return (false, "Selected product is not valid.");
        }

        if (!await context.Branches.AnyAsync(b => b.Id == model.BranchId && b.IsActive, cancellationToken))
        {
            return (false, "Selected branch is not valid.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var inventory = await context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == model.ProductId && i.BranchId == model.BranchId, cancellationToken);

        if (inventory is null)
        {
            if (model.AdjustmentQuantity < 0)
            {
                return (false, "There is no existing stock for this product at this branch to decrease.");
            }

            inventory = new Inventory
            {
                ProductId = model.ProductId,
                BranchId = model.BranchId,
                QuantityOnHand = 0
            };
            context.Inventories.Add(inventory);
        }

        var newQuantity = inventory.QuantityOnHand + model.AdjustmentQuantity;
        if (newQuantity < 0)
        {
            return (false, $"Adjustment would result in negative stock (current on hand: {inventory.QuantityOnHand}).");
        }

        inventory.QuantityOnHand = newQuantity;
        inventory.UpdatedAt = DateTime.UtcNow;

        context.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = model.ProductId,
            BranchId = model.BranchId,
            TransactionType = InventoryTransactionType.Adjustment,
            Quantity = model.AdjustmentQuantity,
            Notes = model.Reason,
            PerformedByUserId = performedByUserId
        });

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (true, null);
    }

    public async Task<List<InventoryBatch>> GetAvailableBatchesAsync(CancellationToken cancellationToken = default) =>
        await context.InventoryBatches
            .Include(b => b.Product)
            .Include(b => b.Branch)
            .Where(b => b.QuantityRemaining > 0)
            .OrderBy(b => b.Product.Name).ThenBy(b => b.BatchNumber)
            .ToListAsync(cancellationToken);

    public async Task<(bool Succeeded, string? Error)> ReportDamagedAsync(DamagedItemViewModel model, string reportedByUserId, CancellationToken cancellationToken = default)
    {
        var batch = await context.InventoryBatches
            .FirstOrDefaultAsync(b => b.Id == model.InventoryBatchId, cancellationToken);

        if (batch is null)
        {
            return (false, "Selected batch was not found.");
        }

        if (model.Quantity > batch.QuantityRemaining)
        {
            return (false, $"Only {batch.QuantityRemaining} unit(s) remain in this batch.");
        }

        var inventory = await context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == batch.ProductId && i.BranchId == batch.BranchId, cancellationToken);

        if (inventory is null || inventory.QuantityOnHand < model.Quantity)
        {
            return (false, "Inventory record is inconsistent with this batch; cannot report damage.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        batch.QuantityRemaining -= model.Quantity;

        inventory.QuantityOnHand -= model.Quantity;
        inventory.UpdatedAt = DateTime.UtcNow;

        context.DamagedItems.Add(new DamagedItem
        {
            ProductId = batch.ProductId,
            BranchId = batch.BranchId,
            InventoryBatchId = batch.Id,
            Quantity = model.Quantity,
            Reason = model.Reason,
            ReportedByUserId = reportedByUserId
        });

        context.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = batch.ProductId,
            BranchId = batch.BranchId,
            InventoryBatchId = batch.Id,
            TransactionType = InventoryTransactionType.Damaged,
            Quantity = -model.Quantity,
            Notes = model.Reason,
            PerformedByUserId = reportedByUserId
        });

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (true, null);
    }

    public async Task<List<DamagedItem>> GetDamagedItemsAsync(CancellationToken cancellationToken = default) =>
        await context.DamagedItems
            .Include(d => d.Product)
            .Include(d => d.Branch)
            .Include(d => d.InventoryBatch)
            .Include(d => d.ReportedByUser)
            .OrderByDescending(d => d.ReportedAt)
            .ToListAsync(cancellationToken);

    public async Task<(bool Succeeded, string? Error)> TransferStockAsync(TransferStockViewModel model, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var sourceBatch = await context.InventoryBatches
            .FirstOrDefaultAsync(b => b.Id == model.SourceInventoryBatchId, cancellationToken);

        if (sourceBatch is null)
        {
            return (false, "Selected source batch was not found.");
        }

        if (sourceBatch.BranchId == model.DestinationBranchId)
        {
            return (false, "Source and destination branch cannot be the same.");
        }

        if (!await context.Branches.AnyAsync(b => b.Id == model.DestinationBranchId && b.IsActive, cancellationToken))
        {
            return (false, "Selected destination branch is not valid.");
        }

        if (model.Quantity > sourceBatch.QuantityRemaining)
        {
            return (false, $"Only {sourceBatch.QuantityRemaining} unit(s) remain in this batch.");
        }

        var sourceInventory = await context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == sourceBatch.ProductId && i.BranchId == sourceBatch.BranchId, cancellationToken);

        if (sourceInventory is null || sourceInventory.QuantityOnHand < model.Quantity)
        {
            return (false, "Inventory record is inconsistent with this batch; cannot transfer.");
        }

        var referenceNumber = $"TRF-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        sourceBatch.QuantityRemaining -= model.Quantity;
        sourceInventory.QuantityOnHand -= model.Quantity;
        sourceInventory.UpdatedAt = DateTime.UtcNow;

        var destinationBatch = new InventoryBatch
        {
            ProductId = sourceBatch.ProductId,
            BranchId = model.DestinationBranchId,
            BatchNumber = sourceBatch.BatchNumber,
            ExpirationDate = sourceBatch.ExpirationDate,
            QuantityReceived = model.Quantity,
            QuantityRemaining = model.Quantity,
            SupplierId = sourceBatch.SupplierId,
            UnitCost = sourceBatch.UnitCost
        };
        context.InventoryBatches.Add(destinationBatch);
        await context.SaveChangesAsync(cancellationToken);

        var destinationInventory = await context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == sourceBatch.ProductId && i.BranchId == model.DestinationBranchId, cancellationToken);

        if (destinationInventory is null)
        {
            destinationInventory = new Inventory
            {
                ProductId = sourceBatch.ProductId,
                BranchId = model.DestinationBranchId,
                QuantityOnHand = 0
            };
            context.Inventories.Add(destinationInventory);
        }

        destinationInventory.QuantityOnHand += model.Quantity;
        destinationInventory.UpdatedAt = DateTime.UtcNow;

        context.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = sourceBatch.ProductId,
            BranchId = sourceBatch.BranchId,
            InventoryBatchId = sourceBatch.Id,
            TransactionType = InventoryTransactionType.TransferOut,
            Quantity = -model.Quantity,
            ReferenceNumber = referenceNumber,
            Notes = model.Notes,
            PerformedByUserId = performedByUserId
        });

        context.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = sourceBatch.ProductId,
            BranchId = model.DestinationBranchId,
            InventoryBatchId = destinationBatch.Id,
            TransactionType = InventoryTransactionType.TransferIn,
            Quantity = model.Quantity,
            ReferenceNumber = referenceNumber,
            Notes = model.Notes,
            PerformedByUserId = performedByUserId
        });

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (true, null);
    }

    public async Task<Sale?> FindSaleByReceiptAsync(string receiptNumber, CancellationToken cancellationToken = default) =>
        await context.Sales
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.ReceiptNumber == receiptNumber, cancellationToken);

    public async Task<(bool Succeeded, string? Error)> ProcessReturnAsync(ProcessReturnViewModel model, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var saleItem = await context.SaleItems
            .Include(si => si.Sale)
            .Include(si => si.Product)
            .FirstOrDefaultAsync(si => si.Id == model.SaleItemId, cancellationToken);

        if (saleItem is null)
        {
            return (false, "Sale item not found.");
        }

        var alreadyReturned = await context.ReturnItems
            .Where(ri => ri.SaleItemId == saleItem.Id)
            .SumAsync(ri => (int?)ri.Quantity, cancellationToken) ?? 0;

        var returnable = saleItem.Quantity - alreadyReturned;
        if (model.Quantity > returnable)
        {
            return (false, $"Only {returnable} unit(s) of {saleItem.Product.Name} can still be returned.");
        }

        var refundAmount = saleItem.UnitPrice * model.Quantity;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var returnRecord = new Return
        {
            SaleId = saleItem.SaleId,
            BranchId = saleItem.Sale.BranchId,
            ProcessedByUserId = performedByUserId,
            Reason = model.Reason,
            TotalRefundAmount = refundAmount,
            ReturnNumber = string.Empty
        };
        context.Returns.Add(returnRecord);
        await context.SaveChangesAsync(cancellationToken);

        returnRecord.ReturnNumber = $"RET-{returnRecord.Id:D6}";

        context.ReturnItems.Add(new ReturnItem
        {
            ReturnId = returnRecord.Id,
            SaleItemId = saleItem.Id,
            ProductId = saleItem.ProductId,
            Quantity = model.Quantity,
            RefundAmount = refundAmount,
            RestockedToInventory = model.RestockToInventory
        });

        if (model.RestockToInventory)
        {
            var inventory = await context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == saleItem.ProductId && i.BranchId == saleItem.Sale.BranchId, cancellationToken);

            if (inventory is null)
            {
                inventory = new Inventory
                {
                    ProductId = saleItem.ProductId,
                    BranchId = saleItem.Sale.BranchId,
                    QuantityOnHand = 0
                };
                context.Inventories.Add(inventory);
            }

            inventory.QuantityOnHand += model.Quantity;
            inventory.UpdatedAt = DateTime.UtcNow;

            context.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = saleItem.ProductId,
                BranchId = saleItem.Sale.BranchId,
                TransactionType = InventoryTransactionType.Return,
                Quantity = model.Quantity,
                ReferenceNumber = returnRecord.ReturnNumber,
                Notes = model.Reason,
                PerformedByUserId = performedByUserId
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (true, null);
    }

    public async Task<List<Return>> GetReturnsAsync(CancellationToken cancellationToken = default) =>
        await context.Returns
            .Include(r => r.Sale)
            .Include(r => r.Branch)
            .Include(r => r.ProcessedByUser)
            .Include(r => r.Items).ThenInclude(ri => ri.Product)
            .OrderByDescending(r => r.ReturnDate)
            .ToListAsync(cancellationToken);
}
