using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Areas.CustomerPortal.Models.ViewModels;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Areas.CustomerPortal.Controllers;

public class RecommendationController(
    ApplicationDbContext context,
    IRecommendationService recommendationService,
    IFeedbackService feedbackService,
    UserManager<ApplicationUser> userManager) : CustomerPortalControllerBase
{
    public async Task<IActionResult> Index()
    {
        return View(new RecommendationRequestViewModel
        {
            Categories = await context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(RecommendationRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            return View(model);
        }

        var userId = userManager.GetUserId(User)!;
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        if (customer is null)
        {
            return NotFound();
        }

        var (succeeded, error, recommendationId) = await recommendationService.GenerateRecommendationAsync(
            customer.Id, model.CategoryId, customer.PreferredBranchId);

        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error!);
            model.Categories = await context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            return View(model);
        }

        return RedirectToAction(nameof(Result), new { id = recommendationId });
    }

    public async Task<IActionResult> Result(int id)
    {
        var recommendation = await recommendationService.GetByIdAsync(id);
        var userId = userManager.GetUserId(User)!;

        // Ownership check: a customer may only view their own recommendation results.
        if (recommendation is null || recommendation.Customer.UserId != userId)
        {
            return NotFound();
        }

        await feedbackService.LogInteractionAsync(recommendation.CustomerId, recommendation.Id, InteractionType.Viewed);

        return View(recommendation);
    }

    public async Task<IActionResult> History()
    {
        var userId = userManager.GetUserId(User)!;
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        if (customer is null)
        {
            return NotFound();
        }

        var history = await context.Recommendations
            .Include(r => r.Category)
            .Where(r => r.CustomerId == customer.Id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(history);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Feedback(int recommendationItemId, int recommendationId, FeedbackRating rating, string? comment)
    {
        var userId = userManager.GetUserId(User)!;
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        if (customer is null)
        {
            return NotFound();
        }

        var (succeeded, error) = await feedbackService.SubmitFeedbackAsync(customer.Id, recommendationItemId, rating, comment);
        TempData[succeeded ? "Success" : "Error"] = succeeded ? "Thanks for your feedback!" : error;

        return RedirectToAction(nameof(Result), new { id = recommendationId });
    }

    // Structured conversational refinement (spec Section 18). Free text is parsed with local
    // keyword detection into a MaxPrice constraint -- a deliberate stand-in for what a real
    // LLM would do for intent extraction (Section 11); everything downstream (filtering,
    // ranking) runs through the exact same pipeline either way.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refine(int id, string? message)
    {
        var current = await recommendationService.GetByIdAsync(id);
        var userId = userManager.GetUserId(User)!;

        if (current is null || current.Customer.UserId != userId)
        {
            return NotFound();
        }

        decimal? maxPrice = null;
        var lower = (message ?? string.Empty).ToLowerInvariant();
        if (lower.Contains("cheap") || lower.Contains("expensive") || lower.Contains("afford") ||
            lower.Contains("budget") || lower.Contains("less") || lower.Contains("lower"))
        {
            var cheapestCurrent = current.Items.Count > 0 ? current.Items.Min(i => i.Product.Price) : (decimal?)null;
            if (cheapestCurrent.HasValue)
            {
                maxPrice = cheapestCurrent.Value - 0.01m;
            }
        }

        var (succeeded, error, newId) = await recommendationService.GenerateRecommendationAsync(
            current.CustomerId, current.CategoryId, current.BranchId, maxPrice);

        if (!succeeded)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Result), new { id });
        }

        return RedirectToAction(nameof(Result), new { id = newId });
    }
}
