using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Helpers;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

public class AccountController : BaseController
{ 
    private readonly IApiClient _api;

    public AccountController(IApiClient api) => _api = api;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.IsLoggedIn() && !HttpContext.Session.IsExpired())
            return RedirectToRoleHome();

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.PostAsync<AuthResponse>(ApiRoutes.Login,
            new LoginRequest { Email = model.Email.Trim(), Password = model.Password });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty,
                result.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest
                    ? "The email or password is incorrect."
                    : result.Error ?? "Login failed. Try again.");
            return View(model);
        }

        var token = result.Data?.ResolvedToken;
        var claims = string.IsNullOrWhiteSpace(token) ? null : TokenReader.Read(token);
        var user = result.Data?.User;

        // The login response says who the user is. The token's claims are the fallback.
        var roleText = !string.IsNullOrWhiteSpace(user?.Role) ? user!.Role : claims?.Role;

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(roleText))
        {
            ModelState.AddModelError(string.Empty, "We couldn't finish logging you in. Please try again.");
            return View(model);
        }

        var info = new TokenInfo(
            user?.UserId ?? claims?.UserId ?? 0,
            !string.IsNullOrWhiteSpace(user?.FullName) ? user!.FullName : claims?.Name ?? "User",
            roleText,
            claims?.ExpiresUtc ?? ToUtc(result.Data?.ExpiresAt) ?? DateTime.UtcNow.AddMinutes(30));

        var role = new[] { Roles.Organiser, Roles.Participant }
            .FirstOrDefault(r => string.Equals(r, info.Role, StringComparison.OrdinalIgnoreCase));

        if (role is null)
        {
            ModelState.AddModelError(string.Empty, "Your account type isn't supported by this site.");
            return View(model);
        }

        HttpContext.Session.Clear();
        HttpContext.Session.SignIn(token, info with { Role = role });

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToRoleHome();
    }

    private static DateTime? ToUtc(DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Local } v => v.ToUniversalTime(),
        { } v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
    };

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.IsLoggedIn() && !HttpContext.Session.IsExpired())
            return RedirectToRoleHome();

        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.PostAsync(ApiRoutes.Register, new RegisterRequest
        {
            FullName = model.Name.Trim(),
            Email = model.Email.Trim(),
            Password = model.Password,
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
            Role = model.Role
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty,
                result.StatusCode == HttpStatusCode.Conflict
                    ? "An account with that email already exists."
                    : result.Error ?? "Registration failed. Try again.");
            return View(model);
        }

        TempData["Success"] = "Your account is ready. Log in to continue.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        TempData["Info"] = "You've been logged out.";
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() => View();
}
