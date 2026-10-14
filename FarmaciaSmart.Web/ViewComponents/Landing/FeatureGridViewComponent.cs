using FarmaciaSmart.Web.Models.ViewModels.Landing;
using Microsoft.AspNetCore.Mvc;

namespace FarmaciaSmart.Web.ViewComponents.Landing;

public class FeatureGridViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(IReadOnlyList<FeatureItem> items) => View(items);
}
