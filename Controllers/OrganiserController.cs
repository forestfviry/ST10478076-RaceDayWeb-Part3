using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Filters;
using RaceDayWeb.Helpers;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

[SessionAuthorize(Roles.Organiser)]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class OrganiserController : BaseController
{
    private readonly IApiClient _api;

    public OrganiserController(IApiClient api) => _api = api;

    // ---------- Dashboard ----------

    public async Task<IActionResult> Dashboard()
    {
        var events = await _api.GetAsync<List<EventDto>>(ApiRoutes.MyEvents);
        if (!events.Success)
        {
            var redirect = AuthRedirect(events);
            if (redirect is not null) return redirect;

            ViewBag.Error = events.Error;
            return View(new DashboardViewModel());
        }

        var model = new DashboardViewModel();
        foreach (var e in (events.Data ?? new()).OrderBy(e => e.EventDate))
        {
            var enrolments = await _api.GetAsync<List<EnrolmentDto>>(ApiRoutes.EventEnrolments(e.EventID));
            model.Rows.Add(new DashboardRow { Event = e, Enrolments = enrolments.Data?.Count ?? 0 });
        }

        return View(model);
    }

    // ---------- Events ----------

    public async Task<IActionResult> Events()
    {
        var result = await _api.GetAsync<List<EventDto>>(ApiRoutes.MyEvents);
        if (!result.Success)
        {
            var redirect = AuthRedirect(result);
            if (redirect is not null) return redirect;

            ViewBag.Error = result.Error;
            return View(new List<EventDto>());
        }

        return View((result.Data ?? new()).OrderBy(e => e.EventDate).ToList());
    }

    [HttpGet]
    public IActionResult CreateEvent() => View("EventForm", new EventFormViewModel());

    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        if (model.EventDate.Date < DateTime.Today)
            ModelState.AddModelError(nameof(model.EventDate), "Choose today or a later date.");
        ValidateBanner(model);
        if (!ModelState.IsValid) return View("EventForm", model);

        var created = await _api.PostAsync<EventDto>(ApiRoutes.Events, ToRequest(model));
        if (!created.Success)
        {
            var redirect = AuthRedirect(created);
            if (redirect is not null) return redirect;

            ModelState.AddModelError(string.Empty, created.Error ?? "The event couldn't be created.");
            return View("EventForm", model);
        }

        TempData["Success"] = $"\"{model.EventName}\" has been created.";
        await UploadBannerIfAny(model, created.Data?.EventID ?? 0);

        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var result = await _api.GetAsync<EventDto>(ApiRoutes.Event(id));
        if (!result.Success || result.Data is null) return LoadFailed(result);

        var e = result.Data;
        return View("EventForm", new EventFormViewModel
        {
            Id = e.EventID,
            EventName = e.EventName,
            Description = e.Description,
            EventDate = e.EventDate.Date,
            Location = e.Location,
            DistanceKm = e.DistanceKm,
            EventType = e.EventType,
            ExistingBannerUrl = e.BannerUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditEvent(int id, EventFormViewModel model)
    {
        model.Id = id;
        ValidateBanner(model);
        if (!ModelState.IsValid) return View("EventForm", model);

        var updated = await _api.PutAsync(ApiRoutes.Event(id), ToRequest(model));
        if (!updated.Success)
        {
            var redirect = AuthRedirect(updated);
            if (redirect is not null) return redirect;

            ModelState.AddModelError(string.Empty, updated.Error ?? "The event couldn't be saved.");
            return View("EventForm", model);
        }

        TempData["Success"] = $"\"{model.EventName}\" has been updated.";
        await UploadBannerIfAny(model, id);

        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await _api.DeleteAsync(ApiRoutes.Event(id));
        if (!result.Success)
        {
            var redirect = AuthRedirect(result);
            if (redirect is not null) return redirect;

            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "The event has been deleted.";
        }

        return RedirectToAction(nameof(Events));
    }

    // ---------- Categories ----------

    [HttpGet]
    public async Task<IActionResult> Categories(int eventId)
    {
        var page = await BuildCategoriesPage(eventId);
        return page.Result ?? View(page.Model);
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(int eventId, [Bind(Prefix = "Form")] CategoryFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            var page = await BuildCategoriesPage(eventId, form);
            return page.Result ?? View("Categories", page.Model);
        }

        var result = await _api.PostAsync(ApiRoutes.EventCategories(eventId), new CategoryRequest
        {
            CategoryName = form.CategoryName.Trim(),
            DistanceKm = form.DistanceKm ?? 0,
            MaxParticipants = form.MaxParticipants ?? 100,
            EntryFee = form.EntryFee ?? 0
        });

        if (!result.Success)
        {
            var redirect = AuthRedirect(result);
            if (redirect is not null) return redirect;

            ModelState.AddModelError(string.Empty, result.Error ?? "The category couldn't be added.");
            var page = await BuildCategoriesPage(eventId, form);
            return page.Result ?? View("Categories", page.Model);
        }

        TempData["Success"] = $"Category \"{form.CategoryName.Trim()}\" added.";
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int eventId, int categoryId)
    {
        var result = await _api.DeleteAsync(ApiRoutes.Category(categoryId));
        if (!result.Success)
        {
            var redirect = AuthRedirect(result);
            if (redirect is not null) return redirect;

            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Category removed.";
        }

        return RedirectToAction(nameof(Categories), new { eventId });
    }

    // ---------- Enrolments ----------

    public async Task<IActionResult> Enrolments(int eventId)
    {
        var ev = await _api.GetAsync<EventDto>(ApiRoutes.Event(eventId));
        if (!ev.Success || ev.Data is null) return LoadFailed(ev);

        var enrolments = await _api.GetAsync<List<EnrolmentDto>>(ApiRoutes.EventEnrolments(eventId));
        if (!enrolments.Success) return LoadFailed(enrolments);

        return View(new EnrolmentsPageViewModel
        {
            Event = ev.Data,
            Enrolments = (enrolments.Data ?? new()).OrderBy(x => x.CategoryName).ThenBy(x => x.ParticipantName).ToList()
        });
    }

    // ---------- Results ----------

    [HttpGet]
    public async Task<IActionResult> Results(int eventId)
    {
        var ev = await _api.GetAsync<EventDto>(ApiRoutes.Event(eventId));
        if (!ev.Success || ev.Data is null) return LoadFailed(ev);

        var enrolments = await _api.GetAsync<List<EnrolmentDto>>(ApiRoutes.EventEnrolments(eventId));
        if (!enrolments.Success) return LoadFailed(enrolments);

        var results = await _api.GetAsync<List<ResultDto>>(ApiRoutes.EventResults(eventId));
        if (!results.Success) return LoadFailed(results);

        var saved = (results.Data ?? new()).ToDictionary(r => r.EnrolmentID);

        return View(new ResultsEntryViewModel
        {
            Event = ev.Data,
            Rows = (enrolments.Data ?? new())
                .Select(e =>
                {
                    saved.TryGetValue(e.EnrolmentID, out var r);
                    return new ResultRowViewModel
                    {
                        EnrolmentId = e.EnrolmentID,
                        ResultId = r?.ResultID,
                        CategoryId = e.CategoryID,
                        CategoryName = e.CategoryName,
                        ParticipantName = e.ParticipantName,
                        FinishTime = r?.FinishTime,
                        Position = r?.Position
                    };
                })
                .OrderBy(r => r.CategoryName)
                .ThenBy(r => r.Position ?? int.MaxValue)
                .ThenBy(r => r.ParticipantName)
                .ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Results(int eventId, ResultsEntryViewModel model)
    {
        var ev = await _api.GetAsync<EventDto>(ApiRoutes.Event(eventId));
        if (!ev.Success || ev.Data is null) return LoadFailed(ev);
        model.Event = ev.Data;

        if (model.Locked)
        {
            ModelState.AddModelError(string.Empty, "Results open on the day of the event.");
            return View(model);
        }

        var times = ValidateResultRows(model);
        if (!ModelState.IsValid) return View(model);

        var saved = 0;
        var failures = new List<string>();

        for (var i = 0; i < model.Rows.Count; i++)
        {
            if (!times.TryGetValue(i, out var time)) continue;

            var row = model.Rows[i];
            var body = new ResultRequest { FinishTime = time, Position = row.Position, Status = "Finished" };

            ApiResult result = row.ResultId is int resultId
                ? await _api.PutAsync(ApiRoutes.Result(resultId), body)
                : await _api.PostAsync(ApiRoutes.EnrolmentResult(row.EnrolmentId), body);

            if (result.Success) { saved++; continue; }

            var redirect = AuthRedirect(result);
            if (redirect is not null) return redirect;

            failures.Add($"{row.ParticipantName}: {result.Error}");
        }

        if (failures.Count == 0)
            TempData["Success"] = saved == 1 ? "1 result published." : $"{saved} results published. Participants can see them now.";
        else
            TempData["Warning"] = $"{saved} saved, {failures.Count} not saved. {string.Join(" ", failures.Take(3))}";

        return RedirectToAction(nameof(Results), new { eventId });
    }

    // ---------- Helpers ----------

    private IActionResult LoadFailed(ApiResult result)
    {
        var redirect = AuthRedirect(result);
        if (redirect is not null) return redirect;

        TempData["Error"] = result.Error ?? "That page couldn't be loaded.";
        return RedirectToAction(nameof(Events));
    }

    private async Task<(CategoriesPageViewModel Model, IActionResult? Result)> BuildCategoriesPage(int eventId, CategoryFormViewModel? form = null)
    {
        var model = new CategoriesPageViewModel { Form = form ?? new CategoryFormViewModel() };

        var ev = await _api.GetAsync<EventDto>(ApiRoutes.Event(eventId));
        if (!ev.Success || ev.Data is null) return (model, LoadFailed(ev));

        var categories = await _api.GetAsync<List<CategoryDto>>(ApiRoutes.EventCategories(eventId));
        if (!categories.Success) return (model, LoadFailed(categories));

        model.Event = ev.Data;
        model.Categories = categories.Data ?? new();
        return (model, null);
    }

    private async Task UploadBannerIfAny(EventFormViewModel model, int eventId)
    {
        if (model.BannerFile is not { Length: > 0 }) return;

        if (eventId <= 0)
        {
            TempData["Warning"] = "The event was saved, but its banner wasn't uploaded. Edit the event to try again.";
            return;
        }

        var upload = await _api.UploadAsync(ApiRoutes.EventBanner(eventId), model.BannerFile);
        if (!upload.Success)
            TempData["Warning"] = $"The event was saved, but the banner didn't upload. {upload.Error}";
    }

    private void ValidateBanner(EventFormViewModel model)
    {
        var error = ImageRules.Validate(model.BannerFile);
        if (error is not null) ModelState.AddModelError(nameof(model.BannerFile), error);
    }

    /// <summary>Checks every row that has a time or position. Returns the cleaned hh:mm:ss time for each row index that is ready to save.</summary>
    private Dictionary<int, string> ValidateResultRows(ResultsEntryViewModel model)
    {
        var ready = new Dictionary<int, string>();
        var seen = new HashSet<(int Category, int Position)>();

        for (var i = 0; i < model.Rows.Count; i++)
        {
            var row = model.Rows[i];
            var hasTime = !string.IsNullOrWhiteSpace(row.FinishTime);
            if (!hasTime && !row.Position.HasValue) continue;

            if (!hasTime || !row.Position.HasValue)
            {
                ModelState.AddModelError($"Rows[{i}].Position", "Enter both a finish time and a position.");
                continue;
            }

            if (!TryNormaliseTime(row.FinishTime!, out var time))
            {
                ModelState.AddModelError($"Rows[{i}].FinishTime", "Use hh:mm:ss, for example 01:32:45.");
                continue;
            }

            if (row.Position.Value < 1)
                ModelState.AddModelError($"Rows[{i}].Position", "Position starts at 1.");
            else if (!seen.Add((row.CategoryId, row.Position.Value)))
                ModelState.AddModelError($"Rows[{i}].Position", $"Position {row.Position} is used twice in {row.CategoryName}.");
            else
                ready[i] = time;
        }

        return ready;
    }

    private static bool TryNormaliseTime(string raw, out string time)
    {
        time = "";
        var m = Regex.Match(raw.Trim(), @"^(\d{1,2}):([0-5]\d):([0-5]\d)$");
        if (!m.Success) return false;

        var h = int.Parse(m.Groups[1].Value);
        if (h > 23) return false;

        var span = new TimeSpan(h, int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value));
        if (span <= TimeSpan.Zero) return false;

        time = span.ToString(@"hh\:mm\:ss");
        return true;
    }

    private static EventRequest ToRequest(EventFormViewModel m) => new()
    {
        EventName = m.EventName.Trim(),
        Description = m.Description?.Trim(),
        EventDate = DateOnly.FromDateTime(m.EventDate),
        Location = m.Location.Trim(),
        EventType = m.EventType,
        DistanceKm = m.DistanceKm
    };
}
