using FarmaciaSmart.Web.Models.ViewModels.Landing;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

/// <summary>
/// Returns the hardcoded marketing content for the public landing page.
/// Registered as a singleton because the data never changes at runtime.
/// </summary>
public sealed class LandingContentService : ILandingContentService
{
    private static readonly IReadOnlyList<TrustItem> TrustItems =
    [
        new("check-circle", "Multi-branch inventory & POS"),
        new("check-circle", "Pharmacist-reviewed AI recommendations"),
        new("check-circle", "Pre-order online, pick up in-store"),
        new("check-circle", "Real-time low-stock & expiry alerts"),
    ];

    private static readonly IReadOnlyList<StepItem> Steps =
    [
        new(1, "Describe your concern", "Tell us your symptoms or what product you're looking for — no medical jargon required."),
        new(2, "Get an AI suggestion", "Our system finds the best OTC match from in-stock items at your chosen branch."),
        new(3, "Pharmacist reviews & you pick up", "A licensed pharmacist reviews every suggestion before you collect it in-store."),
    ];

    private static readonly IReadOnlyList<FeatureItem> Features =
    [
        new("cart", "Shop & Pre-Order", "Browse in-stock products by branch, add them to your cart, and submit a pre-order for pickup.", "#how-it-works"),
        new("sparkles", "AI Recommendations", "Describe a concern and get a preliminary OTC or supplement suggestion, reviewed by a pharmacist.", "#how-it-works"),
        new("building", "Multi-Branch Operations", "One centralized system for inventory, purchasing, and sales across every Farmacia San Antonio branch.", "#features"),
        new("chart", "Staff Tools", "POS, payroll, attendance, and analytics for the team — all in one place.", "#features"),
    ];

    public IReadOnlyList<TrustItem> GetTrustItems() => TrustItems;
    public IReadOnlyList<StepItem> GetSteps() => Steps;
    public IReadOnlyList<FeatureItem> GetFeatures() => Features;
}
