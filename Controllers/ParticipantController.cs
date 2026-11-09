using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Filters;
using RaceDayWeb.Helpers;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

[SessionAuthorize(Roles.Participant)]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ParticipantController : BaseController
{
    private readonly IApiClient _api;

    public ParticipantController(IApiClient api) => _api = api; 

    // ---------- Home: upcoming events with filters ----------

    public async Task<IActionResult> Index(string? q, string? location, string? type, DateTime? from, DateTime? to)
    {
        var result = await _api.GetAsync<List<EventDto>>(ApiRoutes.Events);
        if (!result.Success)
        {
            var redirect = AuthRedirect(result);
            if (redirect is not null) return redirect;

            ViewBag.Error = result.Error;
            return View(new EventListViewModel());
        }

        var all = result.Data ?? new();
        var matches = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            matches = matches.Where(e =>
                e.EventName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (e.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        if (!string.IsNullOrWhiteSpace(location))
            matches = matches.Where(e => string.Equals(e.Location, location, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(type))
            matches = matches.Where(e => string.Equals(e.EventType, type, StringComparison.OrdinalIgnoreCase));
        if (from.HasValue)
            matches = matches.Where(e => e.EventDate.Date >= from.Value.Date);
        if (to.HasValue)
            matches = matches.Where(e => e.EventDate.Date <= to.Value.Date);

        return View(new EventListViewModel
        {
            Events = matches.OrderBy(e => e.EventDate).ToList(),
            TotalUpcoming = all.Count,
            Q = q,
            Location = location,
            Type = type,
            From = from,
            To = to,
            Locations = all.Select(e => e.Location).Where(l => !string.IsNullOrWhiteSpace(l)).Distinct().Order().ToList(),
            Types = all.Select(e => e.EventType).Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t!).Distinct().Order().ToList()
        });
    }

    // ---------- Event details and enrolment ----------

    public async Task<IActionResult> EventDetails(int id)
    {
        var ev = await _api.GetAsync<EventDto>(ApiRoutes.Event(id));
        if (!ev.Success || ev.Data is null) return LoadFailed(ev);

        var categories = await _api.GetAsync<List<CategoryDto>>(ApiRoutes.EventCategories(id));
        if (!categories.Success) return LoadFailed(categories);

        var mine = await _api.GetAsync<List<EnrolmentDto>>(ApiRoutes.MyEnrolments);
        if (mine.IsUnauthorized) return AuthRedirect(mine)!;

        return View(new EventDetailsViewModel
        {
            Event = ev.Data,
            Categories = categories.Data ?? new(),
            EnrolledCategoryIds = (mine.Data ?? new()).Select(e => e.CategoryID).ToHashSet()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Enrol(int categoryId, int eventId)
    {
        var result = await _api.PostAsync(ApiRoutes.Enrol(categoryId), new { });
        if (!result.Success)
        {
            var redirect = AuthRedirect(result);
            if (redirect is not null) return redirect;

            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(EventDetails), new { id = eventId });
        }

        TempData["Success"] = "You're enrolled. Your place is saved.";
        return RedirectToAction(nameof(MyEnrolments));
    }

    // ---------- My enrolments ----------

    public async Task<IActionResult> MyEnrolments()
    {
        var result = await _api.GetAsync<List<EnrolmentDto>>(ApiRoutes.MyEnrolments);
        if (!result.Success)
        {
            var redirect = AuthRedirect(result);
            if (redirect is not null) return redirect;

            ViewBag.Error = result.Error;
            return View(new List<EnrolmentDto>());
        }

        return View(result.Data ?? new());
    }

    [HttpPost]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var result = await _api.DeleteAsync(ApiRoutes.CancelEnrolment(id));
        if (!result.Success)
        {
            var redirect = AuthRedirect(result);
            if (redirect is not null) return redirect;

            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Your enrolment has been cancelled.";
        }

        return RedirectToAction(nameof(MyEnrolments));
    }

    // ---------- My results ----------

    public async Task<IActionResult> MyResults()
    {
        var result = await _api.GetAsync<List<MyResultDto>>(ApiRoutes.MyResults);
        if (!result.Success)
        {
            var redirect = AuthRedirect(result);
            if (redirect is not null) return redirect;

            ViewBag.Error = result.Error;
            return View(new List<MyResultDto>());
        }

        return View((result.Data ?? new()).OrderByDescending(r => r.EventDate).ToList());
    }

    // ---------- Profile ----------

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var page = await BuildProfilePage();
        return page.Result ?? View(page.Model);
    }

    [HttpPost]
    public async Task<IActionResult> Profile([Bind(Prefix = "Form")] ProfileFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            var page = await BuildProfilePage(form);
            return page.Result ?? View(page.Model);
        }

        var result = await _api.PutAsync(ApiRoutes.Me, new UpdateProfileRequest
        {
            FullName = form.FullName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(form.PhoneNumber) ? null : form.PhoneNumber.Trim()
        });

        if (!result.Success)
        {
            var redirect = AuthRedirect(result);
            if (redirect is not null) return redirect;

            ModelState.AddModelError(string.Empty, result.Error ?? "Your details couldn't be saved.");
            var page = await BuildProfilePage(form);
            return page.Result ?? View(page.Model);
        }

        HttpContext.Session.SetString(SessionKeys.UserName, form.FullName.Trim());
        TempData["Success"] = "Your details have been saved.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture is null || picture.Length == 0)
        {
            TempData["Error"] = "Choose an image to upload.";
            return RedirectToAction(nameof(Profile));
        }

        var invalid = ImageRules.Validate(picture);
        if (invalid is not null)
        {
            TempData["Error"] = invalid;
            return RedirectToAction(nameof(Profile));
        }

        var result = await _api.UploadAsync(ApiRoutes.MePicture, picture);
        if (!result.Success)
        {
            var redirect = AuthRedirect(result);
            if (redirect is not null) return redirect;

            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Your profile picture has been updated.";
        }

        return RedirectToAction(nameof(Profile));
    }

    // ---------- Helpers ----------

    private IActionResult LoadFailed(ApiResult result)
    {
        var redirect = AuthRedirect(result);
        if (redirect is not null) return redirect;

        TempData["Error"] = result.Error ?? "That page couldn't be loaded.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<(ProfilePageViewModel Model, IActionResult? Result)> BuildProfilePage(ProfileFormViewModel? form = null)
    {
        var model = new ProfilePageViewModel();

        var me = await _api.GetAsync<UserDto>(ApiRoutes.Me);
        if (!me.Success || me.Data is null)
        {
            var redirect = AuthRedirect(me);
            if (redirect is not null) return (model, redirect);

            TempData["Error"] = me.Error ?? "Your profile couldn't be loaded.";
            return (model, RedirectToAction(nameof(Index)));
        }

        model.User = me.Data;
        model.Form = form ?? new ProfileFormViewModel { FullName = me.Data.FullName, PhoneNumber = me.Data.PhoneNumber };
        return (model, null);
    }
}
