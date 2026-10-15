using FarmaciaSmart.Web.Models.ViewModels.Landing;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

/// <summary>
/// Returns the marketing content for the Farmacia San Antonio public landing page.
/// Registered as a singleton because the static marketing data is immutable at runtime.
/// </summary>
public sealed class LandingContentService : ILandingContentService
{
    private static readonly IReadOnlyList<TrustItem> TrustItems =
    [
        new("building", "Multi-Branch Inventory & In-Store Pickup"),
        new("sparkles", "AI-Powered, Pharmacist-Reviewed Health Guidance"),
        new("cart", "Convenient Online Medicine Pre-Orders"),
        new("chart", "Real-Time Stock, Batch & Expiry Alerts"),
    ];

    private static readonly IReadOnlyList<StepItem> Steps =
    [
        new(1, "Create an account", "Sign up in seconds to access online pre-orders and track order history."),
        new(2, "Browse available products", "Explore pharmacy medicines, OTC essentials, and check branch stock."),
        new(3, "Submit a preorder request", "Select your preferred branch and submit your order securely."),
        new(4, "Receive order confirmation", "Get notified as our licensed pharmacists verify and prepare your request."),
        new(5, "Collect your order at the selected branch", "Pick up your items and complete your transaction conveniently at the counter."),
    ];

    private static readonly IReadOnlyList<FeatureItem> Features =
    [
        new("building", "Multi-Branch Inventory Management", "Unified stock control and batch visibility across all Farmacia San Antonio locations.", "#features"),
        new("sparkles", "AI-Powered OTC Recommendations", "Explore intelligent, pharmacist-verified OTC suggestions and chatbot guidance tailored to your health symptoms.", "/CustomerPortal/Recommendation"),
        new("cart", "Online Pre-Orders and In-Store Pickup", "Browse branch stock in real time, place preorders online, and collect quickly in store.", "#how-it-works"),
        new("chart", "Inventory, Expiry, and Low-Stock Alerts", "Proactive alerts maintain product safety and prevent medicine shortages before they happen.", "#features"),
    ];

    public IReadOnlyList<TrustItem> GetTrustItems() => TrustItems;
    public IReadOnlyList<StepItem> GetSteps() => Steps;
    public IReadOnlyList<FeatureItem> GetFeatures() => Features;
}
