namespace FarmaciaSmart.Web.Models.ViewModels.Landing;

/// <summary>
/// Represents a feature card on the public landing page.
/// </summary>
public sealed record FeatureItem(string Icon, string Title, string Description, string Href);
