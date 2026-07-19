using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;

namespace COSMETICC.Controllers
{
    public class AdminBaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            if (adminId == null)
            {
                context.Result = RedirectToAction("Login", "Admin");
                return;
            }

            var adminRole = HttpContext.Session.GetString("AdminRole") ?? "ADMIN";
            
            // If user is STAFF, restrict access to forbidden controllers
            if (adminRole.Equals("STAFF", StringComparison.OrdinalIgnoreCase))
            {
                var controllerName = context.RouteData.Values["controller"]?.ToString();
                
                // Block STAFF from these controllers
                string[] forbiddenControllersForStaff = new[]
                {
                    "AdminProducts",
                    "AdminCategories",
                    "AdminBrand",
                    "AdminBanner",
                    "AdminVoucher",
                    "AdminSupplier",
                    "AdminBlog"
                };

                if (controllerName != null && forbiddenControllersForStaff.Contains(controllerName))
                {
                    // Set error feedback and redirect to dashboard
                    TempData["ErrorMessage"] = "Bạn không có quyền truy cập vào chức năng này (Quyền của Nhân Viên - STAFF)!";
                    context.Result = RedirectToAction("Dashboard", "Admin");
                    return;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}