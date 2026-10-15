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
    ICustomerCartService cartService,
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
            lower.Contains("budget") || lower.Contains("less") || lower.Contains("lower") || lower.Contains("cheaper"))
        {
            var cheapestCurrent = current.Items.Count > 0 ? current.Items.Min(i => i.Product.Price) : (decimal?)null;
            if (cheapestCurrent.HasValue)
            {
                maxPrice = Math.Max(0, cheapestCurrent.Value - 0.01m);
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

    #region Chatbot AJAX Endpoints

    [HttpGet]
    public async Task<IActionResult> GetInitialState()
    {
        var userId = userManager.GetUserId(User)!;
        var customer = await context.Customers
            .Include(c => c.User)
            .Include(c => c.PreferredBranch)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (customer is null)
        {
            return Unauthorized();
        }

        var categories = await context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new ChatCategoryDto { Id = c.Id, Name = c.Name, Description = c.Description })
            .ToListAsync();

        return Json(new
        {
            customerName = customer.User.FirstName,
            preferredBranch = customer.PreferredBranch?.Name ?? "No branch set",
            categories
        });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ChatMessage([FromBody] ChatMessageRequest request)
    {
        var userId = userManager.GetUserId(User)!;
        var customer = await context.Customers
            .Include(c => c.PreferredBranch)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (customer is null)
        {
            return Unauthorized();
        }

        var userMessage = (request.Message ?? string.Empty).Trim();
        var lower = userMessage.ToLowerInvariant();

        // 1. Check if user is asking for price refinement on an existing recommendation
        var isRefine = lower.Contains("cheap") || lower.Contains("expensive") || lower.Contains("afford") ||
                       lower.Contains("budget") || lower.Contains("less") || lower.Contains("lower") || lower.Contains("cheaper");

        if (request.CurrentRecommendationId.HasValue && isRefine)
        {
            var current = await recommendationService.GetByIdAsync(request.CurrentRecommendationId.Value);
            if (current != null && current.CustomerId == customer.Id)
            {
                decimal? maxPrice = null;
                var cheapestCurrent = current.Items.Count > 0 ? current.Items.Min(i => i.Product.Price) : (decimal?)null;
                if (cheapestCurrent.HasValue)
                {
                    maxPrice = Math.Max(0, cheapestCurrent.Value - 0.01m);
                }

                var (refineOk, refineError, newId) = await recommendationService.GenerateRecommendationAsync(
                    current.CustomerId, current.CategoryId, current.BranchId, maxPrice);

                if (refineOk && newId.HasValue)
                {
                    var newRec = await recommendationService.GetByIdAsync(newId.Value);
                    return Json(new
                    {
                        success = true,
                        isRefinement = true,
                        recommendationId = newRec!.Id,
                        categoryName = newRec.Category.Name,
                        reviewStatus = newRec.ReviewStatus.ToString(),
                        reply = $"I refined the suggestions for **{newRec.Category.Name}** to prioritize more budget-friendly options under ₱{(maxPrice.HasValue ? maxPrice.Value.ToString("N2") : "current range")}:",
                        items = newRec.Items.OrderBy(i => i.Rank).Select(i => new ChatItemDto
                        {
                            Id = i.Id,
                            ProductId = i.ProductId,
                            ProductName = i.Product.Name,
                            Price = i.Product.Price,
                            Explanation = i.Explanation,
                            Warning = i.Warning,
                            Rank = i.Rank,
                            Score = i.Score
                        })
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        recommendationId = current.Id,
                        reply = refineError ?? "No cheaper alternatives were found for this category in current inventory."
                    });
                }
            }
        }

        // 2. Identify Target Category
        int? categoryId = request.CategoryId;
        var activeCategories = await context.Categories.Where(c => c.IsActive).ToListAsync();

        if (!categoryId.HasValue && activeCategories.Count > 0 && !string.IsNullOrWhiteSpace(lower))
        {
            // Exact or substring match on category name
            var matched = activeCategories.FirstOrDefault(c => lower.Contains(c.Name.ToLowerInvariant()));

            if (matched == null)
            {
                // Keyword match
                if (lower.Contains("cough") || lower.Contains("cold") || lower.Contains("flu") || lower.Contains("sipon") || lower.Contains("ubo") || lower.Contains("throat") || lower.Contains("phlegm"))
                {
                    matched = activeCategories.FirstOrDefault(c => c.Name.Contains("Cough", StringComparison.OrdinalIgnoreCase) || c.Name.Contains("Cold", StringComparison.OrdinalIgnoreCase) || c.Name.Contains("Respiratory", StringComparison.OrdinalIgnoreCase));
                }
                else if (lower.Contains("pain") || lower.Contains("headache") || lower.Contains("fever") || lower.Contains("migraine") || lower.Contains("body ache") || lower.Contains("toothache") || lower.Contains("lagnat"))
                {
                    matched = activeCategories.FirstOrDefault(c => c.Name.Contains("Pain", StringComparison.OrdinalIgnoreCase) || c.Name.Contains("Analgesic", StringComparison.OrdinalIgnoreCase) || c.Name.Contains("Fever", StringComparison.OrdinalIgnoreCase));
                }
                else if (lower.Contains("vitamin") || lower.Contains("supplement") || lower.Contains("energy") || lower.Contains("immune") || lower.Contains("ascorbic") || lower.Contains("zinc"))
                {
                    matched = activeCategories.FirstOrDefault(c => c.Name.Contains("Vitamin", StringComparison.OrdinalIgnoreCase) || c.Name.Contains("Supplement", StringComparison.OrdinalIgnoreCase));
                }
                else if (lower.Contains("stomach") || lower.Contains("acid") || lower.Contains("heartburn") || lower.Contains("diarrhea") || lower.Contains("constipation") || lower.Contains("indigestion") || lower.Contains("tiyan"))
                {
                    matched = activeCategories.FirstOrDefault(c => c.Name.Contains("Digestive", StringComparison.OrdinalIgnoreCase) || c.Name.Contains("Gastro", StringComparison.OrdinalIgnoreCase) || c.Name.Contains("Stomach", StringComparison.OrdinalIgnoreCase));
                }
                else if (lower.Contains("allergy") || lower.Contains("itch") || lower.Contains("rash") || lower.Contains("hives") || lower.Contains("allergic") || lower.Contains("kati"))
                {
                    matched = activeCategories.FirstOrDefault(c => c.Name.Contains("Allergy", StringComparison.OrdinalIgnoreCase) || c.Name.Contains("Antihistamine", StringComparison.OrdinalIgnoreCase));
                }
            }

            if (matched != null)
            {
                categoryId = matched.Id;
            }
        }

        if (!categoryId.HasValue)
        {
            if (activeCategories.Count == 0)
            {
                return Json(new
                {
                    success = false,
                    reply = "Our pharmacy catalog is currently being updated. Please consult our on-duty pharmacist directly for guidance."
                });
            }

            return Json(new
            {
                success = false,
                requiresCategory = true,
                reply = "I'm here to provide pharmacist-reviewed OTC recommendations. Please select your health concern or symptom from the list below:",
                categories = activeCategories.Select(c => new ChatCategoryDto { Id = c.Id, Name = c.Name, Description = c.Description })
            });
        }

        var (succeeded, error, recommendationId) = await recommendationService.GenerateRecommendationAsync(
            customer.Id, categoryId.Value, customer.PreferredBranchId);

        if (!succeeded || !recommendationId.HasValue)
        {
            return Json(new
            {
                success = false,
                reply = error ?? "No recommendations could be generated for this concern right now. Please consult the pharmacist directly."
            });
        }

        var recommendation = await recommendationService.GetByIdAsync(recommendationId.Value);

        return Json(new
        {
            success = true,
            recommendationId = recommendation!.Id,
            categoryName = recommendation.Category.Name,
            reviewStatus = recommendation.ReviewStatus.ToString(),
            reply = $"Here are the pharmacist-approved OTC recommendations for **{recommendation.Category.Name}**:",
            items = recommendation.Items.OrderBy(i => i.Rank).Select(i => new ChatItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                Price = i.Product.Price,
                Explanation = i.Explanation,
                Warning = i.Warning,
                Rank = i.Rank,
                Score = i.Score
            })
        });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> FeedbackAjax([FromBody] ChatFeedbackRequest request)
    {
        var userId = userManager.GetUserId(User)!;
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        if (customer is null)
        {
            return Unauthorized();
        }

        var rating = request.Rating == 0 ? FeedbackRating.Helpful : FeedbackRating.NotHelpful;
        var (succeeded, error) = await feedbackService.SubmitFeedbackAsync(customer.Id, request.RecommendationItemId, rating, request.Comment);

        return Json(new { success = succeeded, error });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult AddToCartAjax([FromBody] ChatAddToCartRequest request)
    {
        if (request.Quantity > 0 && request.ProductId > 0)
        {
            cartService.AddItem(request.ProductId, request.ProductName, request.UnitPrice, request.Quantity);
            var cartCount = cartService.GetCart().Sum(i => i.Quantity);
            return Json(new { success = true, cartCount, message = $"Added {request.ProductName} to your cart!" });
        }

        return Json(new { success = false, message = "Invalid product or quantity." });
    }

    #endregion
}
