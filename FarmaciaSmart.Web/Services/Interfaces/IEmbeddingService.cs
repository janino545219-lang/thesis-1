namespace FarmaciaSmart.Web.Services.Interfaces;

public interface IEmbeddingService
{
    float[] GenerateVector(string text);
    double CosineSimilarity(float[] a, float[] b);
    Task<(bool Succeeded, string? Error)> GenerateForProductAsync(int productId, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error, int Count)> GenerateForAllProductsAsync(CancellationToken cancellationToken = default);
}
