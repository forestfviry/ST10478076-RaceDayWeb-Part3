using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Helpers;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

public abstract class BaseController : Controller
{
    /// <summary>If the API says the token is no longer valid, end the session and go to login.</summary>
    protected IActionResult? AuthRedirect(ApiResult result)
    {
        if (result.IsUnauthorized)
        {
            HttpContext.Session.Clear();
            TempData["Info"] = "Your session has expired. Log in again to continue.";
            return RedirectToAction("Login", "Account");
        }

        if (result.IsForbidden)
            return RedirectToAction("AccessDenied", "Account");

        return null;
    }

    protected IActionResult RedirectToRoleHome() =>
        HttpContext.Session.IsInRole(Roles.Organiser)
            ? RedirectToAction("Dashboard", "Organiser")
            : RedirectToAction("Index", "Participant");
}
