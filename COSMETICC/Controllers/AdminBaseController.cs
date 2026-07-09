using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace COSMETICC.Controllers
{
    public class AdminBaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                context.Result = RedirectToAction("Login", "Admin");
            }

            base.OnActionExecuting(context);
        }
    }
}