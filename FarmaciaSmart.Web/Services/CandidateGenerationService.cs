using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Recommendations;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class CandidateGenerationService(ApplicationDbContext context, IEmbeddingService embeddingService) : ICandidateGenerationService
{
    private const int MinDesiredCandidates = 3;

    public async Task<List<CandidatePair>> GenerateAsync(RecommendationContext ctx, CancellationToken cancellationToken = default)
    {
        var rules = await context.RecommendationRules
            .Include(r => r.Product)
            .Where(r => r.IsActive && r.PharmacistApproved && r.CategoryId == ctx.CategoryId)
            .ToListAsync(cancellationToken);

        var candidates = new Dictionary<int, CandidatePair>();

        foreach (var rule in rules)
        {
            List<Product> products;

            if (rule.ProductId.HasValue)
            {
                var product = rule.Product;
                products = product is not null ? [product] : [];
            }
            else
            {
                products = await context.Products
                    .Where(p => p.CategoryId == ctx.CategoryId && p.IsActive && p.IsRecommendationEligible)
                    .ToListAsync(cancellationToken);
            }

            foreach (var product in products)
            {
                if (!IsEligible(product, ctx) || candidates.ContainsKey(product.Id))
                {
                    continue;
                }

                if (!await IsInStockAsync(product.Id, ctx.BranchId, cancellationToken))
                {
                    continue;
                }

                candidates[product.Id] = new CandidatePair { Product = product, Rule = rule };
            }
        }

        // Semantic broadening (Stage 10, spec Section 14): when rule-based matching alone
        // yields too few eligible candidates, widen the pool using local embedding similarity
        // within the same category. Purely additive -- never displaces a rule-based match, and
        // every semantically-added item explicitly discloses it hasn't been pharmacist-reviewed
        // for this specific request (see ExplanationService).
        if (candidates.Count < MinDesiredCandidates)
        {
            var queryText = rules.Count > 0
                ? string.Join(" ", rules.Select(r => r.Trigger))
                : (await context.Categories.FindAsync([ctx.CategoryId], cancellationToken))?.Name ?? string.Empty;

            var queryVector = embeddingService.GenerateVector(queryText);

            var otherProducts = await context.Products
                .Where(p => p.CategoryId == ctx.CategoryId && p.IsActive && p.IsRecommendationEligible)
                .ToListAsync(cancellationToken);

            var scored = new List<(Product Product, double Similarity)>();
            foreach (var product in otherProducts)
            {
                if (!IsEligible(product, ctx) || candidates.ContainsKey(product.Id))
                {
                    continue;
                }

                if (!await IsInStockAsync(product.Id, ctx.BranchId, cancellationToken))
                {
                    continue;
                }

                var embedding = await context.Embeddings.FirstOrDefaultAsync(e => e.ProductId == product.Id, cancellationToken);
                var vector = embedding is not null
                    ? JsonSerializer.Deserialize<float[]>(embedding.Vector) ?? []
                    : embeddingService.GenerateVector($"{product.Name} {product.Brand}");

                scored.Add((product, embeddingService.CosineSimilarity(queryVector, vector)));
            }

            foreach (var (product, similarity) in scored.OrderByDescending(s => s.Similarity).Take(MinDesiredCandidates - candidates.Count))
            {
                candidates[product.Id] = new CandidatePair { Product = product, Rule = null, SimilarityScore = similarity };
            }
        }

        return candidates.Values.ToList();
    }

    private static bool IsEligible(Product product, RecommendationContext ctx) =>
        product.IsActive
        && product.IsRecommendationEligible
        && (!ctx.MaxPrice.HasValue || product.Price <= ctx.MaxPrice.Value);

    private async Task<bool> IsInStockAsync(int productId, int branchId, CancellationToken cancellationToken) =>
        await context.Inventories.AnyAsync(i => i.ProductId == productId && i.BranchId == branchId && i.QuantityOnHand > 0, cancellationToken);
}
