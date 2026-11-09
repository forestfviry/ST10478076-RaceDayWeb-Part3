using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDayWeb.Helpers;

namespace RaceDayWeb.Filters;

/// <summary>
/// Blocks a page unless the session holds a valid login. Pass roles to restrict it further,
/// e.g. [SessionAuthorize(Roles.Organiser)].
/// </summary>
public class SessionAuthorizeAttribute : ActionFilterAttribute
{
    private readonly string[] _roles;
     
    public SessionAuthorizeAttribute(params string[] roles) => _roles = roles;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var tempData = (context.Controller as Controller)?.TempData;

        if (!session.IsLoggedIn() || session.IsExpired())
        {
            var wasLoggedIn = session.IsLoggedIn();
            session.Clear();

            if (wasLoggedIn && tempData is not null)
                tempData["Info"] = "Your session has expired. Log in again to continue.";

            var request = context.HttpContext.Request;
            var returnUrl = HttpMethods.IsGet(request.Method) ? request.Path + request.QueryString : null;

            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (_roles.Length > 0 && !_roles.Contains(session.GetRole(), StringComparer.OrdinalIgnoreCase))
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}
