using FarmaciaSmart.Web.Models.ViewModels.Landing;
using Microsoft.AspNetCore.Mvc;

namespace FarmaciaSmart.Web.ViewComponents.Landing;

public class TrustBarViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(IReadOnlyList<TrustItem> items) => View(items);
}
