using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

/// <summary>
/// Local, no-API-key text embedding via character-trigram feature hashing into a fixed-size
/// vector -- a real, working similarity signal (products with similar names/descriptions get
/// similar vectors), not a mock. Spec Section 15 explicitly allows the embedding model to
/// remain configurable; this is the free/local default, swappable for a real embedding API
/// later without touching any caller (they all go through IEmbeddingService).
/// </summary>
public class LocalHashingEmbeddingService(ApplicationDbContext context) : IEmbeddingService
{
    private const int Dimensions = 128;

    public float[] GenerateVector(string text)
    {
        var vector = new float[Dimensions];
        var normalized = (text ?? string.Empty).ToLowerInvariant();

        for (var i = 0; i < normalized.Length - 2; i++)
        {
            var trigram = normalized.Substring(i, 3);
            var index = Math.Abs(trigram.GetHashCode()) % Dimensions;
            vector[index] += 1;
        }

        var magnitude = Math.Sqrt(vector.Sum(v => (double)v * v));
        if (magnitude > 0)
        {
            for (var i = 0; i < Dimensions; i++)
            {
                vector[i] = (float)(vector[i] / magnitude);
            }
        }

        return vector;
    }

    public double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0, magA = 0, magB = 0;
        var length = Math.Min(a.Length, b.Length);

        for (var i = 0; i < length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        if (magA == 0 || magB == 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }

    public async Task<(bool Succeeded, string? Error)> GenerateForProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null)
        {
            return (false, "Product not found.");
        }

        var sourceText = $"{product.Name} {product.Category.Name} {product.Brand}".Trim();
        var vectorJson = JsonSerializer.Serialize(GenerateVector(sourceText));

        var existing = await context.Embeddings.FirstOrDefaultAsync(e => e.ProductId == productId, cancellationToken);
        if (existing is null)
        {
            context.Embeddings.Add(new Embedding { ProductId = productId, Vector = vectorJson, SourceText = sourceText });
        }
        else
        {
            existing.Vector = vectorJson;
            existing.SourceText = sourceText;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error, int Count)> GenerateForAllProductsAsync(CancellationToken cancellationToken = default)
    {
        var productIds = await context.Products.Where(p => p.IsActive).Select(p => p.Id).ToListAsync(cancellationToken);

        var count = 0;
        foreach (var id in productIds)
        {
            var (succeeded, _) = await GenerateForProductAsync(id, cancellationToken);
            if (succeeded)
            {
                count++;
            }
        }

        return (true, null, count);
    }
}
