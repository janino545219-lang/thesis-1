namespace FarmaciaSmart.Web.Models.Entities;

/// <summary>
/// One row per customer: explicitly-stated preferences (spec Section 8/9), currently just the
/// budget ceiling learned from conversational refinement (Section 18). Recalled as a default by
/// IContextService on future requests where the customer doesn't restate a constraint.
/// </summary>
public class CustomerPreference
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public decimal? MaxBudget { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
