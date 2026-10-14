namespace FarmaciaSmart.Web.Models.Entities;

public enum InteractionType
{
    Viewed = 0,
    Dismissed = 1
}

public class RecommendationInteraction
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int RecommendationId { get; set; }
    public Recommendation Recommendation { get; set; } = null!;

    public InteractionType InteractionType { get; set; }
    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
