using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Suppliers;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers;

[Authorize(Roles = $"{ApplicationRoles.Administrator},{ApplicationRoles.InventoryStaff}")]
public class SupplierController(
    ISupplierService supplierService,
    IAuditLogService auditLogService,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(await supplierService.GetAllAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var supplier = await supplierService.GetByIdAsync(id);
        return supplier is null ? NotFound() : View(supplier);
    }

    public IActionResult Create()
    {
        return View(new SupplierViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (succeeded, error) = await supplierService.CreateAsync(model);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error!);
            return View(model);
        }

        await auditLogService.LogAsync("SupplierCreated", userManager.GetUserId(User), $"Name: {model.Name}");
        TempData["Success"] = "Supplier created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var supplier = await supplierService.GetByIdAsync(id);
        if (supplier is null)
        {
            return NotFound();
        }

        return View(new SupplierViewModel
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactPerson = supplier.ContactPerson,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Address = supplier.Address
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SupplierViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (succeeded, error) = await supplierService.UpdateAsync(model);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error!);
            return View(model);
        }

        await auditLogService.LogAsync("SupplierUpdated", userManager.GetUserId(User), $"Id: {model.Id}");
        TempData["Success"] = "Supplier updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, bool isActive)
    {
        if (await supplierService.SetActiveStatusAsync(id, isActive))
        {
            await auditLogService.LogAsync(isActive ? "SupplierActivated" : "SupplierDeactivated", userManager.GetUserId(User), $"Id: {id}");
        }

        return RedirectToAction(nameof(Index));
    }
}
