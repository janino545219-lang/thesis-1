namespace FarmaciaSmart.Web.Models.ViewModels.Recommendations;

public class RecommendationContext
{
    public int CustomerId { get; set; }
    public int CategoryId { get; set; }
    public int BranchId { get; set; }

    /// <summary>Optional budget constraint, e.g. from conversational refinement ("that's too expensive").</summary>
    public decimal? MaxPrice { get; set; }
}
