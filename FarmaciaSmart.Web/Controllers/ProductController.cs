using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Products;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers;

[Authorize(Roles = $"{ApplicationRoles.Administrator},{ApplicationRoles.BranchManager},{ApplicationRoles.InventoryStaff}")]
public class ProductController(
    IProductService productService,
    ApplicationDbContext context,
    IAuditLogService auditLogService,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(await productService.GetAllAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await productService.GetByIdAsync(id);
        return product is null ? NotFound() : View(product);
    }

    public async Task<IActionResult> Create()
    {
        return View(new ProductViewModel { Categories = await GetActiveCategoriesAsync() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await GetActiveCategoriesAsync();
            return View(model);
        }

        var (succeeded, error) = await productService.CreateAsync(model);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error!);
            model.Categories = await GetActiveCategoriesAsync();
            return View(model);
        }

        await auditLogService.LogAsync("ProductCreated", userManager.GetUserId(User), $"Name: {model.Name}");
        TempData["Success"] = "Product created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await productService.GetByIdAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        return View(new ProductViewModel
        {
            Id = product.Id,
            Name = product.Name,
            CategoryId = product.CategoryId,
            Brand = product.Brand,
            Unit = product.Unit,
            Barcode = product.Barcode,
            Price = product.Price,
            ProductType = product.ProductType,
            RequiresExpirationTracking = product.RequiresExpirationTracking,
            LowStockThreshold = product.LowStockThreshold,
            IsRecommendationEligible = product.IsRecommendationEligible,
            Categories = await GetActiveCategoriesAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await GetActiveCategoriesAsync();
            return View(model);
        }

        var (succeeded, error) = await productService.UpdateAsync(model);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error!);
            model.Categories = await GetActiveCategoriesAsync();
            return View(model);
        }

        await auditLogService.LogAsync("ProductUpdated", userManager.GetUserId(User), $"Id: {model.Id}");
        TempData["Success"] = "Product updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, bool isActive)
    {
        if (await productService.SetActiveStatusAsync(id, isActive))
        {
            await auditLogService.LogAsync(isActive ? "ProductActivated" : "ProductDeactivated", userManager.GetUserId(User), $"Id: {id}");
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<List<Category>> GetActiveCategoriesAsync() =>
        await context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
}
