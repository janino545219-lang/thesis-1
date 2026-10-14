using FarmaciaSmart.Web.Models.ViewModels.Recommendations;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

/// <summary>
/// Stage 9/10: a pure pass-through of the pharmacist-authored rule text -- never fabricated.
/// A real LLM (Stage 10+, not yet wired up -- see project notes) would rephrase this more
/// naturally, but per spec Section 17 it must still retrieve facts from here rather than
/// inventing them, so this remains the source of truth either way.
/// </summary>
public class ExplanationService : IExplanationService
{
    private const string UnreviewedExplanation =
        "This product is commonly associated with similar concerns based on its description and category, " +
        "though it has not been specifically reviewed by a pharmacist for this request.";

    private const string UnreviewedWarning =
        "This suggestion has not been reviewed by a pharmacist. Please ask our pharmacist before use.";

    public (string Explanation, string? Warning) Explain(CandidatePair candidate) =>
        candidate.Rule is not null
            ? (candidate.Rule.Explanation, candidate.Rule.Warning)
            : (UnreviewedExplanation, UnreviewedWarning);
}
