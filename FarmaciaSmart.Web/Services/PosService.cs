using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Sales;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class PosService(ApplicationDbContext context) : IPosService
{
    public async Task<(bool Succeeded, string? Error, int? SaleId)> CheckoutAsync(
        List<CartItemDto> cartItems, CheckoutViewModel model, int branchId, string cashierUserId, CancellationToken cancellationToken = default)
    {
        if (cartItems.Count == 0)
        {
            return (false, "Cart is empty.", null);
        }

        if (!await context.Branches.AnyAsync(b => b.Id == branchId && b.IsActive, cancellationToken))
        {
            return (false, "Operating branch is not valid.", null);
        }

        // Validate every line before making any changes, so checkout is all-or-nothing.
        var inventoryByProduct = new Dictionary<int, Inventory>();
        foreach (var item in cartItems)
        {
            var product = await context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId && p.IsActive, cancellationToken);
            if (product is null)
            {
                return (false, $"{item.ProductName} is no longer available.", null);
            }

            var inventory = await context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.BranchId == branchId, cancellationToken);

            if (inventory is null || inventory.QuantityOnHand < item.Quantity)
            {
                var available = inventory?.QuantityOnHand ?? 0;
                return (false, $"Insufficient stock for {item.ProductName} (available: {available}, requested: {item.Quantity}).", null);
            }

            inventoryByProduct[item.ProductId] = inventory;
        }

        var subTotal = cartItems.Sum(i => i.LineTotal);
        var discountAmount = model.ApplySeniorPwdDiscount ? Math.Round(subTotal * 0.20m, 2) : 0m;
        var totalAmount = subTotal - discountAmount;

        if (model.AmountTendered < totalAmount)
        {
            return (false, "Amount tendered is less than the total amount due.", null);
        }

        Customer? customer = null;
        if (!string.IsNullOrWhiteSpace(model.CustomerContactNumber))
        {
            customer = await context.Customers
                .FirstOrDefaultAsync(c => c.ContactNumber == model.CustomerContactNumber, cancellationToken);
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var sale = new Sale
        {
            BranchId = branchId,
            CustomerId = customer?.Id,
            CashierUserId = cashierUserId,
            SubTotal = subTotal,
            DiscountAmount = discountAmount,
            TotalAmount = totalAmount,
            ReceiptNumber = string.Empty
        };
        context.Sales.Add(sale);
        await context.SaveChangesAsync(cancellationToken);

        sale.ReceiptNumber = $"SALE-{sale.Id:D6}";

        foreach (var item in cartItems)
        {
            context.SaleItems.Add(new SaleItem
            {
                SaleId = sale.Id,
                ProductId = item.ProductId,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                LineTotal = item.LineTotal
            });

            var inventory = inventoryByProduct[item.ProductId];
            inventory.QuantityOnHand -= item.Quantity;
            inventory.UpdatedAt = DateTime.UtcNow;

            context.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = item.ProductId,
                BranchId = branchId,
                TransactionType = InventoryTransactionType.Sale,
                Quantity = -item.Quantity,
                ReferenceNumber = sale.ReceiptNumber,
                PerformedByUserId = cashierUserId
            });
        }

        context.Payments.Add(new Payment
        {
            SaleId = sale.Id,
            Method = model.PaymentMethod,
            AmountTendered = model.AmountTendered,
            ChangeAmount = model.AmountTendered - totalAmount
        });

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (true, null, sale.Id);
    }

    public async Task<Sale?> GetSaleAsync(int saleId, CancellationToken cancellationToken = default) =>
        await context.Sales
            .Include(s => s.Branch)
            .Include(s => s.Customer).ThenInclude(c => c!.User)
            .Include(s => s.CashierUser)
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .Include(s => s.Payment)
            .FirstOrDefaultAsync(s => s.Id == saleId, cancellationToken);

    public async Task<List<Sale>> GetSalesHistoryAsync(int? branchId = null, CancellationToken cancellationToken = default)
    {
        var query = context.Sales
            .Include(s => s.Branch)
            .Include(s => s.CashierUser)
            .Include(s => s.Payment)
            .AsQueryable();

        if (branchId.HasValue)
        {
            query = query.Where(s => s.BranchId == branchId.Value);
        }

        return await query.OrderByDescending(s => s.SaleDate).Take(200).ToListAsync(cancellationToken);
    }
}
