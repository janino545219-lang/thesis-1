using FarmaciaSmart.Web.Models.ViewModels.Recommendations;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface IExplanationService
{
    (string Explanation, string? Warning) Explain(CandidatePair candidate);
}
