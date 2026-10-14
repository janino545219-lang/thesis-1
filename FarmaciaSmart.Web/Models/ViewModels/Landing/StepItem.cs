namespace FarmaciaSmart.Web.Models.ViewModels.Landing;

/// <summary>
/// Represents a numbered step in the "How It Works" section of the landing page.
/// </summary>
public sealed record StepItem(int Number, string Title, string Description);
