using Microsoft.AspNetCore.Mvc;

namespace FarmaciaSmart.Web.ViewComponents.Landing;

public class LandingFooterViewComponent : ViewComponent
{
    public IViewComponentResult Invoke() => View();
}
