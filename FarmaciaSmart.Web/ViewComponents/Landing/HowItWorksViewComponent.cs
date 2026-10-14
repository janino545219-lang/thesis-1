using FarmaciaSmart.Web.Models.ViewModels.Landing;
using Microsoft.AspNetCore.Mvc;

namespace FarmaciaSmart.Web.ViewComponents.Landing;

public class HowItWorksViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(IReadOnlyList<StepItem> items) => View(items);
}
