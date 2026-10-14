using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Inventories;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers;

[Authorize(Roles = $"{ApplicationRoles.Administrator},{ApplicationRoles.InventoryStaff}")]
public class InventoryController(
    IInventoryService inventoryService,
    ApplicationDbContext context,
    IAuditLogService auditLogService,
    INotificationService notificationService,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(await inventoryService.GetStockAsync());
    }

    public async Task<IActionResult> History(int page = 1)
    {
        return View(await inventoryService.GetRecentTransactionsAsync(page));
    }

    public async Task<IActionResult> StockIn()
    {
        return View(await BuildStockInViewModelAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StockIn(StockInViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildStockInViewModelAsync(model));
        }

        var userId = userManager.GetUserId(User)!;
        var (succeeded, error) = await inventoryService.StockInAsync(model, userId);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error!);
            return View(await BuildStockInViewModelAsync(model));
        }

        await auditLogService.LogAsync(
            "InventoryStockIn",
            userId,
            $"ProductId: {model.ProductId}, BranchId: {model.BranchId}, Qty: {model.Quantity}, Batch: {model.BatchNumber}");

        TempData["Success"] = "Stock received successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Adjust()
    {
        return View(await BuildAdjustmentViewModelAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Adjust(StockAdjustmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildAdjustmentViewModelAsync(model));
        }

        var userId = userManager.GetUserId(User)!;
        var (succeeded, error) = await inventoryService.AdjustStockAsync(model, userId);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error!);
            return View(await BuildAdjustmentViewModelAsync(model));
        }

        await auditLogService.LogAsync(
            "InventoryAdjustment",
            userId,
            $"ProductId: {model.ProductId}, BranchId: {model.BranchId}, Delta: {model.AdjustmentQuantity}, Reason: {model.Reason}");

        TempData["Success"] = "Stock adjustment recorded successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ReportDamaged()
    {
        return View(await BuildDamagedItemViewModelAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportDamaged(DamagedItemViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildDamagedItemViewModelAsync(model));
        }

        var userId = userManager.GetUserId(User)!;
        var (succeeded, error) = await inventoryService.ReportDamagedAsync(model, userId);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error!);
            return View(await BuildDamagedItemViewModelAsync(model));
        }

        await auditLogService.LogAsync(
            "InventoryDamagedReported",
            userId,
            $"BatchId: {model.InventoryBatchId}, Qty: {model.Quantity}, Reason: {model.Reason}");

        var batch = await context.InventoryBatches
            .Include(b => b.Product).Include(b => b.Branch)
            .FirstOrDefaultAsync(b => b.Id == model.InventoryBatchId);
        if (batch is not null)
        {
            await notificationService.NotifyAsync(
                NotificationType.DamagedItem,
                $"Damaged item: {batch.Product.Name} at {batch.Branch.Name}",
                $"{model.Quantity} unit(s) of {batch.Product.Name} reported damaged at {batch.Branch.Name}. Reason: {model.Reason}",
                batch.BranchId);
        }

        TempData["Success"] = "Damaged item recorded successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> DamagedItems()
    {
        return View(await inventoryService.GetDamagedItemsAsync());
    }

    public async Task<IActionResult> Transfer()
    {
        return View(await BuildTransferViewModelAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Transfer(TransferStockViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildTransferViewModelAsync(model));
        }

        var userId = userManager.GetUserId(User)!;
        var (succeeded, error) = await inventoryService.TransferStockAsync(model, userId);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error!);
            return View(await BuildTransferViewModelAsync(model));
        }

        await auditLogService.LogAsync(
            "InventoryTransfer",
            userId,
            $"SourceBatchId: {model.SourceInventoryBatchId}, DestinationBranchId: {model.DestinationBranchId}, Qty: {model.Quantity}");

        TempData["Success"] = "Stock transferred successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Return(string? receiptNumber)
    {
        var model = new ProcessReturnViewModel();
        if (!string.IsNullOrWhiteSpace(receiptNumber))
        {
            model.ReceiptNumber = receiptNumber;
            model.Sale = await inventoryService.FindSaleByReceiptAsync(receiptNumber);
            if (model.Sale is null)
            {
                ModelState.AddModelError(string.Empty, "No sale found with that receipt number.");
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(ProcessReturnViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Sale = await inventoryService.FindSaleByReceiptAsync(model.ReceiptNumber);
            return View(model);
        }

        var userId = userManager.GetUserId(User)!;
        var (succeeded, error) = await inventoryService.ProcessReturnAsync(model, userId);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error!);
            model.Sale = await inventoryService.FindSaleByReceiptAsync(model.ReceiptNumber);
            return View(model);
        }

        await auditLogService.LogAsync(
            "InventoryReturnProcessed",
            userId,
            $"SaleItemId: {model.SaleItemId}, Qty: {model.Quantity}, Reason: {model.Reason}");

        var saleItem = await context.SaleItems
            .Include(si => si.Product)
            .Include(si => si.Sale).ThenInclude(s => s.Branch)
            .FirstOrDefaultAsync(si => si.Id == model.SaleItemId);
        if (saleItem is not null)
        {
            await notificationService.NotifyAsync(
                NotificationType.Return,
                $"Return: {saleItem.Product.Name} at {saleItem.Sale.Branch.Name}",
                $"{model.Quantity} unit(s) of {saleItem.Product.Name} returned at {saleItem.Sale.Branch.Name}. Reason: {model.Reason}",
                saleItem.Sale.BranchId);
        }

        TempData["Success"] = "Return processed successfully.";
        return RedirectToAction(nameof(Returns));
    }

    public async Task<IActionResult> Returns()
    {
        return View(await inventoryService.GetReturnsAsync());
    }

    private async Task<StockInViewModel> BuildStockInViewModelAsync(StockInViewModel? model = null)
    {
        model ??= new StockInViewModel();
        model.Products = await context.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
        model.Branches = await context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
        model.Suppliers = await context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
        return model;
    }

    private async Task<StockAdjustmentViewModel> BuildAdjustmentViewModelAsync(StockAdjustmentViewModel? model = null)
    {
        model ??= new StockAdjustmentViewModel();
        model.Products = await context.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
        model.Branches = await context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
        return model;
    }

    private async Task<DamagedItemViewModel> BuildDamagedItemViewModelAsync(DamagedItemViewModel? model = null)
    {
        model ??= new DamagedItemViewModel();
        model.AvailableBatches = await inventoryService.GetAvailableBatchesAsync();
        return model;
    }

    private async Task<TransferStockViewModel> BuildTransferViewModelAsync(TransferStockViewModel? model = null)
    {
        model ??= new TransferStockViewModel();
        model.AvailableBatches = await inventoryService.GetAvailableBatchesAsync();
        model.Branches = await context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
        return model;
    }
}
