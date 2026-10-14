using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Models.ViewModels.Recommendations;

public class CandidatePair
{
    public Product Product { get; set; } = null!;

    /// <summary>Null when the candidate came from semantic broadening rather than a matched rule.</summary>
    public RecommendationRule? Rule { get; set; }

    public double SimilarityScore { get; set; }
}
