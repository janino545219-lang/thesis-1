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
/// REST surface for the pharmacist-derived knowledge base (Section 13/38). Additive alongside the
/// existing RecommendationRuleController Razor pages -- same data, same authorization, same
/// validation rules, just a JSON/DTO shape instead of views.
/// </summary>
[ApiController]
[Route("api/recommendation-rules")]
[Authorize(Roles = $"{ApplicationRoles.Administrator},{ApplicationRoles.Pharmacist}")]
public class RecommendationRulesController(
    ApplicationDbContext context,
    IAuditLogService auditLogService,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var rules = await context.RecommendationRules
            .Include(r => r.Category)
            .Include(r => r.Product)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(rules.Select(MapToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var rule = await context.RecommendationRules
            .Include(r => r.Category)
            .Include(r => r.Product)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return rule is null ? NotFound() : Ok(MapToDto(rule));
    }

    [HttpPost]
    public async Task<IActionResult> Create(RecommendationRuleWriteDto dto, CancellationToken cancellationToken)
    {
        if (!await context.Categories.AnyAsync(c => c.Id == dto.CategoryId && c.IsActive, cancellationToken))
        {
            return BadRequest(new { error = "Selected category is not valid." });
        }

        if (dto.ProductId.HasValue && !await context.Products.AnyAsync(p => p.Id == dto.ProductId && p.IsActive, cancellationToken))
        {
            return BadRequest(new { error = "Selected product is not valid." });
        }

        var userId = userManager.GetUserId(User)!;

        var rule = new RecommendationRule
        {
            Trigger = dto.Trigger,
            CategoryId = dto.CategoryId,
            ProductId = dto.ProductId,
            RecommendationType = dto.RecommendationType,
            Warning = dto.Warning,
            Restriction = dto.Restriction,
            Explanation = dto.Explanation,
            PharmacistApproved = dto.PharmacistApproved,
            CreatedByUserId = userId
        };

        context.RecommendationRules.Add(rule);
        await context.SaveChangesAsync(cancellationToken);
        await auditLogService.LogAsync("RecommendationRuleCreated", userId, $"Trigger: {dto.Trigger}", cancellationToken);

        await context.Entry(rule).Reference(r => r.Category).LoadAsync(cancellationToken);
        if (rule.ProductId.HasValue)
        {
            await context.Entry(rule).Reference(r => r.Product).LoadAsync(cancellationToken);
        }

        return CreatedAtAction(nameof(GetById), new { id = rule.Id }, MapToDto(rule));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, RecommendationRuleWriteDto dto, CancellationToken cancellationToken)
    {
        var rule = await context.RecommendationRules
            .Include(r => r.Category)
            .Include(r => r.Product)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (rule is null)
        {
            return NotFound();
        }

        if (!await context.Categories.AnyAsync(c => c.Id == dto.CategoryId && c.IsActive, cancellationToken))
        {
            return BadRequest(new { error = "Selected category is not valid." });
        }

        if (dto.ProductId.HasValue && !await context.Products.AnyAsync(p => p.Id == dto.ProductId && p.IsActive, cancellationToken))
        {
            return BadRequest(new { error = "Selected product is not valid." });
        }

        rule.Trigger = dto.Trigger;
        rule.CategoryId = dto.CategoryId;
        rule.ProductId = dto.ProductId;
        rule.RecommendationType = dto.RecommendationType;
        rule.Warning = dto.Warning;
        rule.Restriction = dto.Restriction;
        rule.Explanation = dto.Explanation;
        rule.PharmacistApproved = dto.PharmacistApproved;
        rule.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        await auditLogService.LogAsync("RecommendationRuleUpdated", userManager.GetUserId(User), $"Id: {id}", cancellationToken);

        await context.Entry(rule).Reference(r => r.Category).LoadAsync(cancellationToken);
        rule.Product = dto.ProductId.HasValue
            ? await context.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId, cancellationToken)
            : null;

        return Ok(MapToDto(rule));
    }

    private static RecommendationRuleDto MapToDto(RecommendationRule r) => new()
    {
        Id = r.Id,
        Trigger = r.Trigger,
        CategoryId = r.CategoryId,
        CategoryName = r.Category.Name,
        ProductId = r.ProductId,
        ProductName = r.Product?.Name,
        RecommendationType = r.RecommendationType,
        Warning = r.Warning,
        Restriction = r.Restriction,
        Explanation = r.Explanation,
        PharmacistApproved = r.PharmacistApproved,
        Active = r.IsActive,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };
}
