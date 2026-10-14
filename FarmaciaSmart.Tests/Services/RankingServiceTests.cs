using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Recommendations;
using FarmaciaSmart.Web.Services;

namespace FarmaciaSmart.Tests.Services;

public class RankingServiceTests
{
    private static (Category category, Product product1, Product product2, Product product3) SeedProducts(ApplicationDbContext context)
    {
        var category = new Category { Name = "Pain Relief", IsActive = true };
        context.Categories.Add(category);
        context.SaveChanges();

        var p1 = new Product { Name = "Biogesic", CategoryId = category.Id, Price = 85.5m, IsActive = true };
        var p2 = new Product { Name = "Generic Paracetamol", CategoryId = category.Id, Price = 25m, IsActive = true };
        var p3 = new Product { Name = "Alaxan FR", CategoryId = category.Id, Price = 95m, IsActive = true };
        context.Products.AddRange(p1, p2, p3);
        context.SaveChanges();

        return (category, p1, p2, p3);
    }

    [Fact]
    public async Task RankAsync_ProductSpecificRule_OutranksCategoryWideRule()
    {
        using var context = TestDbContextFactory.Create();
        var (category, product1, product2, _) = SeedProducts(context);

        var specificRule = new RecommendationRule
        {
            Trigger = "Headache", CategoryId = category.Id, ProductId = product1.Id,
            RecommendationType = "OTC", Explanation = "Specific match.", PharmacistApproved = true, CreatedByUserId = "u1"
        };
        var categoryRule = new RecommendationRule
        {
            Trigger = "Headache", CategoryId = category.Id, ProductId = null,
            RecommendationType = "OTC", Explanation = "Category-wide match.", PharmacistApproved = true, CreatedByUserId = "u1"
        };

        var candidates = new List<CandidatePair>
        {
            new() { Product = product2, Rule = categoryRule },
            new() { Product = product1, Rule = specificRule }
        };

        var service = new RankingService(context);
        var ranked = await service.RankAsync(customerId: 1, candidates);

        Assert.Equal(product1.Id, ranked[0].Candidate.Product.Id);
        Assert.True(ranked[0].Score > ranked[1].Score);
    }

    [Fact]
    public async Task RankAsync_RuleMatch_OutranksSemanticOnlyMatchEvenWithHighSimilarity()
    {
        using var context = TestDbContextFactory.Create();
        var (category, product1, _, product3) = SeedProducts(context);

        var rule = new RecommendationRule
        {
            Trigger = "Headache", CategoryId = category.Id, ProductId = product1.Id,
            RecommendationType = "OTC", Explanation = "...", PharmacistApproved = true, CreatedByUserId = "u1"
        };

        var candidates = new List<CandidatePair>
        {
            new() { Product = product3, Rule = null, SimilarityScore = 0.9 },
            new() { Product = product1, Rule = rule }
        };

        var service = new RankingService(context);
        var ranked = await service.RankAsync(customerId: 1, candidates);

        Assert.Equal(product1.Id, ranked[0].Candidate.Product.Id);
    }

    [Fact]
    public async Task RankAsync_CustomerNotHelpfulFeedback_LowersFutureScoreForThatProductOnly()
    {
        using var context = TestDbContextFactory.Create();
        var (category, product1, product2, _) = SeedProducts(context);
        var branch = new Branch { Name = "Main Branch", Code = "MAIN", Address = "123 St", IsActive = true };
        context.Branches.Add(branch);
        context.SaveChanges();

        var customer = new Customer { UserId = "user-1", Status = CustomerStatus.Active };
        context.Customers.Add(customer);
        context.SaveChanges();

        var recommendation = new Recommendation { CustomerId = customer.Id, CategoryId = category.Id, BranchId = branch.Id, RequestText = "test" };
        context.Recommendations.Add(recommendation);
        context.SaveChanges();

        var item = new RecommendationItem { RecommendationId = recommendation.Id, ProductId = product1.Id, Rank = 1, Score = 0.5m, Explanation = "..." };
        context.RecommendationItems.Add(item);
        context.SaveChanges();

        context.RecommendationFeedbacks.Add(new RecommendationFeedback
        {
            RecommendationItemId = item.Id,
            CustomerId = customer.Id,
            Rating = FeedbackRating.NotHelpful
        });
        context.SaveChanges();

        var candidates = new List<CandidatePair>
        {
            new() { Product = product1, Rule = null, SimilarityScore = 0.5 },
            new() { Product = product2, Rule = null, SimilarityScore = 0.5 }
        };

        var service = new RankingService(context);
        var ranked = await service.RankAsync(customer.Id, candidates);

        // Base score for both = 0.5 * 5 = 2.5. product1 gets -3 for the NotHelpful vote (net -0.5);
        // product2 has no feedback and keeps its base 2.5, so it should now rank above product1.
        var product1Result = ranked.Single(r => r.Candidate.Product.Id == product1.Id);
        var product2Result = ranked.Single(r => r.Candidate.Product.Id == product2.Id);

        Assert.Equal(-0.5m, product1Result.Score);
        Assert.Equal(2.5m, product2Result.Score);
        Assert.Equal(product2.Id, ranked[0].Candidate.Product.Id);
    }

    [Fact]
    public async Task RankAsync_FeedbackFromDifferentCustomer_DoesNotAffectThisCustomersRanking()
    {
        using var context = TestDbContextFactory.Create();
        var (category, product1, _, _) = SeedProducts(context);
        var branch = new Branch { Name = "Main Branch", Code = "MAIN", Address = "123 St", IsActive = true };
        context.Branches.Add(branch);
        context.SaveChanges();

        var otherCustomer = new Customer { UserId = "other-user", Status = CustomerStatus.Active };
        var thisCustomer = new Customer { UserId = "this-user", Status = CustomerStatus.Active };
        context.Customers.AddRange(otherCustomer, thisCustomer);
        context.SaveChanges();

        var recommendation = new Recommendation { CustomerId = otherCustomer.Id, CategoryId = category.Id, BranchId = branch.Id, RequestText = "test" };
        context.Recommendations.Add(recommendation);
        context.SaveChanges();

        var item = new RecommendationItem { RecommendationId = recommendation.Id, ProductId = product1.Id, Rank = 1, Score = 0m, Explanation = "..." };
        context.RecommendationItems.Add(item);
        context.SaveChanges();

        context.RecommendationFeedbacks.Add(new RecommendationFeedback
        {
            RecommendationItemId = item.Id,
            CustomerId = otherCustomer.Id,
            Rating = FeedbackRating.NotHelpful
        });
        context.SaveChanges();

        var candidates = new List<CandidatePair> { new() { Product = product1, Rule = null, SimilarityScore = 0.5 } };
        var service = new RankingService(context);
        var ranked = await service.RankAsync(thisCustomer.Id, candidates);

        // The other customer's NotHelpful vote must not leak into thisCustomer's ranking.
        Assert.Equal(2.5m, ranked[0].Score);
    }
}
