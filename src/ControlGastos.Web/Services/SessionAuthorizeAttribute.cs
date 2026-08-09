using ControlGastos.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ControlGastos.Web.Services;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.HttpContext.Session.GetString(SessionKeys.AccessToken)))
        {
            return;
        }

        var returnUrl = context.HttpContext.Request.PathBase + context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
        context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
    }
}
