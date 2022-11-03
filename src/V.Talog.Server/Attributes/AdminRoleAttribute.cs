using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace V.Talog.Server.Attributes
{
    public class AdminRoleAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.HttpContext.Items.ContainsKey("role"))
            {
                context.Result = new StatusCodeResult(401);
                return;
            }
            var role = context.HttpContext.Items["role"]?.ToString();
            if (role != "admin")
            {
                context.Result = new StatusCodeResult(401);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
