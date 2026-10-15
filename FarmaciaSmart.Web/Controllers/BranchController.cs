using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Branches;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers;

[Authorize(Roles = ApplicationRoles.AdminOrSuper)]
public class BranchController(
    IBranchService branchService,
    IAuditLogService auditLogService,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(await branchService.GetAllAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var branch = await branchService.GetByIdAsync(id);
        return branch is null ? NotFound() : View(branch);
    }

    public IActionResult Create()
    {
        return View(new BranchViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BranchViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (succeeded, error) = await branchService.CreateAsync(model);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error!);
            return View(model);
        }

        await auditLogService.LogAsync("BranchCreated", userManager.GetUserId(User), $"Name: {model.Name}");
        TempData["Success"] = "Branch created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var branch = await branchService.GetByIdAsync(id);
        if (branch is null)
        {
            return NotFound();
        }

        return View(new BranchViewModel
        {
            Id = branch.Id,
            Name = branch.Name,
            Code = branch.Code,
            Address = branch.Address,
            ContactNumber = branch.ContactNumber
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BranchViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (succeeded, error) = await branchService.UpdateAsync(model);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error!);
            return View(model);
        }

        await auditLogService.LogAsync("BranchUpdated", userManager.GetUserId(User), $"Id: {model.Id}");
        TempData["Success"] = "Branch updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, bool isActive)
    {
        if (await branchService.SetActiveStatusAsync(id, isActive))
        {
            await auditLogService.LogAsync(isActive ? "BranchActivated" : "BranchDeactivated", userManager.GetUserId(User), $"Id: {id}");
        }

        return RedirectToAction(nameof(Index));
    }
}
