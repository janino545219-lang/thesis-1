using FarmaciaSmart.Web.Models.ViewModels.Landing;

namespace FarmaciaSmart.Web.Services.Interfaces;

/// <summary>
/// Provides the content displayed on the public landing page.
/// All items are returned from C# so they are easy to unit-test and localize later.
/// </summary>
public interface ILandingContentService
{
    IReadOnlyList<TrustItem> GetTrustItems();
    IReadOnlyList<StepItem> GetSteps();
    IReadOnlyList<FeatureItem> GetFeatures();
}
