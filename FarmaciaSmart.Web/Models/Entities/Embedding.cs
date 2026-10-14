namespace FarmaciaSmart.Web.Models.Entities;

public class Embedding
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>JSON-serialized float array. Local hashing-based vector -- see LocalHashingEmbeddingService.</summary>
    public string Vector { get; set; } = string.Empty;
    public string SourceText { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
