using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers;

[Authorize(Roles = $"{ApplicationRoles.Administrator},{ApplicationRoles.Pharmacist}")]
public class RecommendationReviewController(
    IRecommendationService recommendationService,
    IAuditLogService auditLogService,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(await recommendationService.GetPendingReviewAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var recommendation = await recommendationService.GetByIdAsync(id);
        return recommendation is null ? NotFound() : View(recommendation);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int id, RecommendationReviewStatus status, string? notes)
    {
        var userId = userManager.GetUserId(User)!;
        var (succeeded, error) = await recommendationService.ReviewAsync(id, status, userId, notes);

        if (succeeded)
        {
            await auditLogService.LogAsync("RecommendationReviewed", userId, $"RecommendationId: {id}, Status: {status}");
            TempData["Success"] = "Recommendation reviewed.";
        }
        else
        {
            TempData["Error"] = error;
        }

        return RedirectToAction(nameof(Index));
    }
}
