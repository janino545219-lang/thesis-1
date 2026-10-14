using Microsoft.AspNetCore.Mvc;

namespace FarmaciaSmart.Web.ViewComponents.Landing;

public class HeroSectionViewComponent : ViewComponent
{
    public IViewComponentResult Invoke() => View();
}
