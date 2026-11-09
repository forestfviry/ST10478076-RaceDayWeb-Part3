using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Helpers;

namespace RaceDayWeb.Controllers;

public class HomeController : BaseController
{
    public IActionResult Index()
    {
        if (HttpContext.Session.IsLoggedIn() && !HttpContext.Session.IsExpired())
            return RedirectToRoleHome();

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();

    public IActionResult Status(int code)
    {
        ViewBag.Code = code;
        return View();
    }
}
