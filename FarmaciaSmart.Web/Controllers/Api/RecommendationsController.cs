using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.DTOs.Recommendations;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers.Api;

/// <summary>
/// The REST surface required by spec Section 2/38 for the Recommendation module. Additive only:
/// calls the exact same services (IRecommendationService, IFeedbackService, ...) the existing
/// CustomerPortal Razor pages use, so nothing about the working MVC flow changes. Auth here is
/// still the app's cookie-based Identity auth (no separate token scheme), so a caller needs an
/// authenticated session the same way the portal does.
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public class RecommendationsController(
    IRecommendationService recommendationService,
    IFeedbackService feedbackService,
    ApplicationDbContext context,
    IAuditLogService auditLogService,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpPost("recommendations")]
    [Authorize(Roles = ApplicationRoles.Customer)]
    public async Task<IActionResult> Create(RecommendationRequestDto dto, CancellationToken cancellationToken)
    {
        var customer = await GetCallingCustomerAsync(cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        var (succeeded, error, recommendationId) = await recommendationService.GenerateRecommendationAsync(
            customer.Id, dto.CategoryId, dto.BranchId ?? customer.PreferredBranchId, dto.MaxPrice, cancellationToken);

        if (!succeeded)
        {
            return BadRequest(new { error });
        }

        var recommendation = await recommendationService.GetByIdAsync(recommendationId!.Value, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = recommendationId }, MapToDto(recommendation!));
    }

    [HttpGet("recommendations/{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var recommendation = await recommendationService.GetByIdAsync(id, cancellationToken);
        if (recommendation is null)
        {
            return NotFound();
        }

        if (!await CanAccessCustomerDataAsync(recommendation.CustomerId, cancellationToken))
        {
            return NotFound();
        }

        return Ok(MapToDto(recommendation));
    }

    [HttpGet("/api/customers/{customerId:int}/recommendations")]
    public async Task<IActionResult> GetByCustomer(int customerId, CancellationToken cancellationToken)
    {
        if (!await CanAccessCustomerDataAsync(customerId, cancellationToken))
        {
            return NotFound();
        }

        var recommendations = await context.Recommendations
            .Include(r => r.Category)
            .Include(r => r.Branch)
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(recommendations.Select(r => new RecommendationDto
        {
            Id = r.Id,
            CustomerId = r.CustomerId,
            CategoryId = r.CategoryId,
            CategoryName = r.Category.Name,
            BranchId = r.BranchId,
            BranchName = r.Branch.Name,
            RequestText = r.RequestText,
            ReviewStatus = r.ReviewStatus.ToString(),
            ReviewNotes = r.ReviewNotes,
            ReviewedAt = r.ReviewedAt,
            CreatedAt = r.CreatedAt
        }));
    }

    [HttpPost("recommendations/{id:int}/feedback")]
    [Authorize(Roles = ApplicationRoles.Customer)]
    public async Task<IActionResult> SubmitFeedback(int id, FeedbackRequestDto dto, CancellationToken cancellationToken)
    {
        var customer = await GetCallingCustomerAsync(cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        var (succeeded, error) = await feedbackService.SubmitFeedbackAsync(
            customer.Id, dto.RecommendationItemId, dto.Rating, dto.Comment, cancellationToken);

        if (!succeeded)
        {
            return NotFound(new { error });
        }

        return NoContent();
    }

    [HttpPost("recommendations/{id:int}/review")]
    [Authorize(Roles = $"{ApplicationRoles.Administrator},{ApplicationRoles.Pharmacist}")]
    public async Task<IActionResult> Review(int id, ReviewRequestDto dto, CancellationToken cancellationToken)
    {
        if (dto.Status == RecommendationReviewStatus.PendingReview)
        {
            return BadRequest(new { error = "Invalid review status." });
        }

        var userId = userManager.GetUserId(User)!;
        var (succeeded, error) = await recommendationService.ReviewAsync(id, dto.Status, userId, dto.Notes, cancellationToken);

        if (!succeeded)
        {
            return NotFound(new { error });
        }

        await auditLogService.LogAsync("RecommendationReviewed", userId, $"RecommendationId: {id}, Status: {dto.Status}", cancellationToken);

        var recommendation = await recommendationService.GetByIdAsync(id, cancellationToken);
        return Ok(MapToDto(recommendation!));
    }

    private async Task<Customer?> GetCallingCustomerAsync(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        return await context.Customers.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }

    /// <summary>A customer may only read their own data; Administrator/Pharmacist may read any (oversight, Section 20).</summary>
    private async Task<bool> CanAccessCustomerDataAsync(int customerId, CancellationToken cancellationToken)
    {
        if (User.IsInRole(ApplicationRoles.Administrator) || User.IsInRole(ApplicationRoles.Pharmacist))
        {
            return true;
        }

        var customer = await GetCallingCustomerAsync(cancellationToken);
        return customer is not null && customer.Id == customerId;
    }

    private static RecommendationDto MapToDto(Recommendation r) => new()
    {
        Id = r.Id,
        CustomerId = r.CustomerId,
        CategoryId = r.CategoryId,
        CategoryName = r.Category.Name,
        BranchId = r.BranchId,
        BranchName = r.Branch.Name,
        RequestText = r.RequestText,
        ReviewStatus = r.ReviewStatus.ToString(),
        ReviewNotes = r.ReviewNotes,
        ReviewedAt = r.ReviewedAt,
        CreatedAt = r.CreatedAt,
        Items = [.. r.Items.OrderBy(i => i.Rank).Select(i => new RecommendationItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.Product.Name,
            Price = i.Product.Price,
            Rank = i.Rank,
            Score = i.Score,
            Explanation = i.Explanation,
            Warning = i.Warning,
            PharmacistRuleBased = i.RecommendationRuleId.HasValue,
            FeedbackRating = i.Feedback?.Rating.ToString()
        })]
    };
}
