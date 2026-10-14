using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Recommendations;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers;

[Authorize(Roles = $"{ApplicationRoles.Administrator},{ApplicationRoles.Pharmacist}")]
public class RecommendationRuleController(
    ApplicationDbContext context,
    IEmbeddingService embeddingService,
    IAuditLogService auditLogService,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateEmbeddings()
    {
        var (succeeded, error, count) = await embeddingService.GenerateForAllProductsAsync();

        if (succeeded)
        {
            TempData["Success"] = $"Generated embeddings for {count} product(s).";
            await auditLogService.LogAsync("ProductEmbeddingsGenerated", userManager.GetUserId(User), $"Count: {count}");
        }
        else
        {
            TempData["Error"] = error;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Index()
    {
        return View(await context.RecommendationRules
            .Include(r => r.Category)
            .Include(r => r.Product)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var rule = await context.RecommendationRules
            .Include(r => r.Category)
            .Include(r => r.Product)
            .Include(r => r.CreatedByUser)
            .FirstOrDefaultAsync(r => r.Id == id);

        return rule is null ? NotFound() : View(rule);
    }

    public async Task<IActionResult> Create()
    {
        return View(await BuildViewModelAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RecommendationRuleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildViewModelAsync(model));
        }

        if (!await context.Categories.AnyAsync(c => c.Id == model.CategoryId && c.IsActive))
        {
            ModelState.AddModelError(string.Empty, "Selected category is not valid.");
            return View(await BuildViewModelAsync(model));
        }

        if (model.ProductId.HasValue && !await context.Products.AnyAsync(p => p.Id == model.ProductId && p.IsActive))
        {
            ModelState.AddModelError(string.Empty, "Selected product is not valid.");
            return View(await BuildViewModelAsync(model));
        }

        var userId = userManager.GetUserId(User)!;

        context.RecommendationRules.Add(new RecommendationRule
        {
            Trigger = model.Trigger,
            CategoryId = model.CategoryId,
            ProductId = model.ProductId,
            RecommendationType = model.RecommendationType,
            Warning = model.Warning,
            Restriction = model.Restriction,
            Explanation = model.Explanation,
            PharmacistApproved = model.PharmacistApproved,
            CreatedByUserId = userId
        });

        await context.SaveChangesAsync();

        await auditLogService.LogAsync("RecommendationRuleCreated", userId, $"Trigger: {model.Trigger}");
        TempData["Success"] = "Recommendation rule created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var rule = await context.RecommendationRules.FirstOrDefaultAsync(r => r.Id == id);
        if (rule is null)
        {
            return NotFound();
        }

        return View(await BuildViewModelAsync(new RecommendationRuleViewModel
        {
            Id = rule.Id,
            Trigger = rule.Trigger,
            CategoryId = rule.CategoryId,
            ProductId = rule.ProductId,
            RecommendationType = rule.RecommendationType,
            Warning = rule.Warning,
            Restriction = rule.Restriction,
            Explanation = rule.Explanation,
            PharmacistApproved = rule.PharmacistApproved
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RecommendationRuleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildViewModelAsync(model));
        }

        var rule = await context.RecommendationRules.FirstOrDefaultAsync(r => r.Id == model.Id);
        if (rule is null)
        {
            return NotFound();
        }

        if (!await context.Categories.AnyAsync(c => c.Id == model.CategoryId && c.IsActive))
        {
            ModelState.AddModelError(string.Empty, "Selected category is not valid.");
            return View(await BuildViewModelAsync(model));
        }

        rule.Trigger = model.Trigger;
        rule.CategoryId = model.CategoryId;
        rule.ProductId = model.ProductId;
        rule.RecommendationType = model.RecommendationType;
        rule.Warning = model.Warning;
        rule.Restriction = model.Restriction;
        rule.Explanation = model.Explanation;
        rule.PharmacistApproved = model.PharmacistApproved;
        rule.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        await auditLogService.LogAsync("RecommendationRuleUpdated", userManager.GetUserId(User), $"Id: {model.Id}");
        TempData["Success"] = "Recommendation rule updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, bool isActive)
    {
        var rule = await context.RecommendationRules.FirstOrDefaultAsync(r => r.Id == id);
        if (rule is not null)
        {
            rule.IsActive = isActive;
            rule.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            await auditLogService.LogAsync(
                isActive ? "RecommendationRuleActivated" : "RecommendationRuleDeactivated",
                userManager.GetUserId(User),
                $"Id: {id}");
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<RecommendationRuleViewModel> BuildViewModelAsync(RecommendationRuleViewModel? model = null)
    {
        model ??= new RecommendationRuleViewModel();
        model.Categories = await context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
        model.Products = await context.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
        return model;
    }
}
