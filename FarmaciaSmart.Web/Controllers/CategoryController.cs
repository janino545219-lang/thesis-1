using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Categories;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers;

[Authorize(Roles = $"{ApplicationRoles.Administrator},{ApplicationRoles.InventoryStaff}")]
public class CategoryController(
    ICategoryService categoryService,
    IAuditLogService auditLogService,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(await categoryService.GetAllAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var category = await categoryService.GetByIdAsync(id);
        return category is null ? NotFound() : View(category);
    }

    public IActionResult Create()
    {
        return View(new CategoryViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (succeeded, error) = await categoryService.CreateAsync(model);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error!);
            return View(model);
        }

        await auditLogService.LogAsync("CategoryCreated", userManager.GetUserId(User), $"Name: {model.Name}");
        TempData["Success"] = "Category created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var category = await categoryService.GetByIdAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        return View(new CategoryViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (succeeded, error) = await categoryService.UpdateAsync(model);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error!);
            return View(model);
        }

        await auditLogService.LogAsync("CategoryUpdated", userManager.GetUserId(User), $"Id: {model.Id}");
        TempData["Success"] = "Category updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, bool isActive)
    {
        if (await categoryService.SetActiveStatusAsync(id, isActive))
        {
            await auditLogService.LogAsync(isActive ? "CategoryActivated" : "CategoryDeactivated", userManager.GetUserId(User), $"Id: {id}");
        }

        return RedirectToAction(nameof(Index));
    }
}
