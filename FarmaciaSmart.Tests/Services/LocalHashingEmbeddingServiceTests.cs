using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services;

namespace FarmaciaSmart.Tests.Services;

public class LocalHashingEmbeddingServiceTests
{
    [Fact]
    public void GenerateVector_SameInput_IsDeterministic()
    {
        using var context = TestDbContextFactory.Create();
        var service = new LocalHashingEmbeddingService(context);

        var v1 = service.GenerateVector("Biogesic Paracetamol 500mg Pain Relief");
        var v2 = service.GenerateVector("Biogesic Paracetamol 500mg Pain Relief");

        Assert.Equal(v1, v2);
    }

    [Fact]
    public void GenerateVector_IsCaseInsensitive()
    {
        using var context = TestDbContextFactory.Create();
        var service = new LocalHashingEmbeddingService(context);

        var lower = service.GenerateVector("biogesic paracetamol");
        var mixed = service.GenerateVector("Biogesic Paracetamol");

        Assert.Equal(lower, mixed);
    }

    [Fact]
    public void GenerateVector_IsNormalizedToUnitLength()
    {
        using var context = TestDbContextFactory.Create();
        var service = new LocalHashingEmbeddingService(context);

        var vector = service.GenerateVector("Amoxicillin 500mg Antibiotic Capsule");
        var magnitude = Math.Sqrt(vector.Sum(v => (double)v * v));

        Assert.True(Math.Abs(magnitude - 1.0) < 0.0001);
    }

    [Fact]
    public void GenerateVector_EmptyString_ReturnsZeroVectorWithoutThrowing()
    {
        using var context = TestDbContextFactory.Create();
        var service = new LocalHashingEmbeddingService(context);

        var vector = service.GenerateVector("");

        Assert.All(vector, v => Assert.Equal(0f, v));
    }

    [Fact]
    public void CosineSimilarity_SimilarText_ScoresHigherThanDissimilarText()
    {
        using var context = TestDbContextFactory.Create();
        var service = new LocalHashingEmbeddingService(context);

        var baseVector = service.GenerateVector("Biogesic Paracetamol 500mg Pain Relief Tablet");
        var similarVector = service.GenerateVector("Biogesic Paracetamol 500mg Pain Relief Caplet");
        var dissimilarVector = service.GenerateVector("Zzzquil Diphenhydramine Sleep Aid Liquid");

        var similarScore = service.CosineSimilarity(baseVector, similarVector);
        var dissimilarScore = service.CosineSimilarity(baseVector, dissimilarVector);

        Assert.True(similarScore > dissimilarScore);
    }

    [Fact]
    public void CosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        using var context = TestDbContextFactory.Create();
        var service = new LocalHashingEmbeddingService(context);

        var vector = service.GenerateVector("Alaxan FR Ibuprofen Paracetamol");
        var score = service.CosineSimilarity(vector, vector);

        Assert.True(Math.Abs(score - 1.0) < 0.0001);
    }

    [Fact]
    public void CosineSimilarity_ZeroVector_ReturnsZeroWithoutThrowing()
    {
        using var context = TestDbContextFactory.Create();
        var service = new LocalHashingEmbeddingService(context);

        var zero = new float[128];
        var nonZero = service.GenerateVector("Biogesic");

        Assert.Equal(0, service.CosineSimilarity(zero, nonZero));
        Assert.Equal(0, service.CosineSimilarity(zero, zero));
    }

    [Fact]
    public void CosineSimilarity_ResultIsWithinExpectedRange()
    {
        using var context = TestDbContextFactory.Create();
        var service = new LocalHashingEmbeddingService(context);

        var a = service.GenerateVector("Biogesic Paracetamol Pain Relief");
        var b = service.GenerateVector("Amoxicillin Antibiotic Capsule");
        var score = service.CosineSimilarity(a, b);

        Assert.InRange(score, -1.0001, 1.0001);
    }

    [Fact]
    public async Task GenerateForProductAsync_NonExistentProduct_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var service = new LocalHashingEmbeddingService(context);

        var (succeeded, error) = await service.GenerateForProductAsync(999);

        Assert.False(succeeded);
        Assert.Equal("Product not found.", error);
    }

    [Fact]
    public async Task GenerateForProductAsync_CreatesEmbeddingFromNameCategoryAndBrand()
    {
        using var context = TestDbContextFactory.Create();
        var category = new Category { Name = "Pain Relief", IsActive = true };
        context.Categories.Add(category);
        context.SaveChanges();
        var product = new Product { Name = "Biogesic", CategoryId = category.Id, Brand = "Unilab", Price = 85.5m, IsActive = true };
        context.Products.Add(product);
        context.SaveChanges();

        var service = new LocalHashingEmbeddingService(context);
        var (succeeded, error) = await service.GenerateForProductAsync(product.Id);

        Assert.True(succeeded);
        Assert.Null(error);
        var embedding = context.Embeddings.Single(e => e.ProductId == product.Id);
        Assert.Equal("Biogesic Pain Relief Unilab", embedding.SourceText);
    }

    [Fact]
    public async Task GenerateForProductAsync_CalledTwice_UpdatesExistingEmbeddingRatherThanDuplicating()
    {
        using var context = TestDbContextFactory.Create();
        var category = new Category { Name = "Pain Relief", IsActive = true };
        context.Categories.Add(category);
        context.SaveChanges();
        var product = new Product { Name = "Biogesic", CategoryId = category.Id, Brand = "Unilab", Price = 85.5m, IsActive = true };
        context.Products.Add(product);
        context.SaveChanges();

        var service = new LocalHashingEmbeddingService(context);
        await service.GenerateForProductAsync(product.Id);
        product.Name = "Biogesic 500mg";
        context.SaveChanges();
        await service.GenerateForProductAsync(product.Id);

        Assert.Single(context.Embeddings);
        Assert.Equal("Biogesic 500mg Pain Relief Unilab", context.Embeddings.Single().SourceText);
    }

    [Fact]
    public async Task GenerateForAllProductsAsync_OnlyProcessesActiveProducts()
    {
        using var context = TestDbContextFactory.Create();
        var category = new Category { Name = "Pain Relief", IsActive = true };
        context.Categories.Add(category);
        context.SaveChanges();
        context.Products.AddRange(
            new Product { Name = "Biogesic", CategoryId = category.Id, Price = 85.5m, IsActive = true },
            new Product { Name = "Alaxan", CategoryId = category.Id, Price = 95m, IsActive = true },
            new Product { Name = "Discontinued Item", CategoryId = category.Id, Price = 10m, IsActive = false });
        context.SaveChanges();

        var service = new LocalHashingEmbeddingService(context);
        var (succeeded, error, count) = await service.GenerateForAllProductsAsync();

        Assert.True(succeeded);
        Assert.Null(error);
        Assert.Equal(2, count);
        Assert.Equal(2, context.Embeddings.Count());
    }
}
