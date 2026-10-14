using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FarmaciaSmart.Web.Helpers;

namespace FarmaciaSmart.Web.Areas.CustomerPortal.Controllers;

[Area("CustomerPortal")]
[Authorize(Roles = ApplicationRoles.Customer)]
public abstract class CustomerPortalControllerBase : Controller
{
}
