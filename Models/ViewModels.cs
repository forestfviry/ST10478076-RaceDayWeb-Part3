using System.ComponentModel.DataAnnotations;
using RaceDayWeb.Helpers;

namespace RaceDayWeb.Models;

// ---------- Account ----------

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your email."), EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Enter your password."), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Enter your email."), EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = "";

    [RegularExpression(@"^[0-9+\s()\-]{7,20}$", ErrorMessage = "Enter a valid phone number.")]
    [Display(Name = "Phone number (optional)")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Choose a password."), DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Use at least 8 characters.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Repeat your password."), DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords don't match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";

    [Required, RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Choose how you'll use RaceDay.")]
    public string Role { get; set; } = Roles.Participant;
}

// ---------- Organiser ----------

public class EventFormViewModel
{
    public static readonly string[] EventTypes = { "Road run", "Trail run", "Cycling", "Walk", "Triathlon" };

    public int Id { get; set; }

    [Required(ErrorMessage = "Enter an event name."), StringLength(100)]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = "";

    [Required(ErrorMessage = "Add a short description."), StringLength(500, ErrorMessage = "Keep the description under 500 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Choose the event date."), DataType(DataType.Date)]
    [Display(Name = "Event date")]
    public DateTime EventDate { get; set; } = DateTime.Today.AddDays(30);

    [Required(ErrorMessage = "Enter the location."), StringLength(150)]
    public string Location { get; set; } = "";

    [Required(ErrorMessage = "Enter the distance."), Range(0.1, 1000, ErrorMessage = "Enter a distance between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Choose an event type.")]
    [Display(Name = "Event type")]
    public string? EventType { get; set; }

    public string? ExistingBannerUrl { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? BannerFile { get; set; }
}

public class DashboardRow
{
    public EventDto Event { get; set; } = new();
    public int Enrolments { get; set; }
    public bool IsUpcoming => Event.EventDate.Date >= DateTime.Today;
}

public class DashboardViewModel
{
    public List<DashboardRow> Rows { get; set; } = new();
    public int TotalEnrolments => Rows.Sum(r => r.Enrolments);
    public int UpcomingCount => Rows.Count(r => r.IsUpcoming);
    public DashboardRow? Next => Rows.Where(r => r.IsUpcoming).OrderBy(r => r.Event.EventDate).FirstOrDefault();
}

public class CategoryFormViewModel
{
    [Required(ErrorMessage = "Name the category."), StringLength(50)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = "";

    [Required(ErrorMessage = "Enter the distance."), Range(0.1, 1000, ErrorMessage = "Enter a distance between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Enter the maximum number of participants."), Range(1, 100000, ErrorMessage = "Enter a number between 1 and 100000.")]
    [Display(Name = "Maximum participants")]
    public int? MaxParticipants { get; set; } = 100;

    [Required(ErrorMessage = "Enter the entry fee (0 for free)."), Range(0, 100000, ErrorMessage = "Enter a fee of 0 or more.")]
    [Display(Name = "Entry fee (R)")]
    public decimal? EntryFee { get; set; } = 0;
}

public class CategoriesPageViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public CategoryFormViewModel Form { get; set; } = new();
}

public class EnrolmentsPageViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class ResultRowViewModel
{
    public int EnrolmentId { get; set; }
    public int? ResultId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string ParticipantName { get; set; } = "";

    [Display(Name = "Finish time")]
    public string? FinishTime { get; set; }

    public int? Position { get; set; }
}

public class ResultsEntryViewModel
{
    public EventDto Event { get; set; } = new();
    public List<ResultRowViewModel> Rows { get; set; } = new();

    /// <summary>Results can only be entered once the event day has arrived.</summary>
    public bool Locked => Event.EventDate.Date > DateTime.Today;
}

// ---------- Participant ----------

public class EventListViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public int TotalUpcoming { get; set; }

    public string? Q { get; set; }
    public string? Location { get; set; }
    public string? Type { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    public List<string> Locations { get; set; } = new();
    public List<string> Types { get; set; } = new();

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Q) || !string.IsNullOrWhiteSpace(Location) || !string.IsNullOrWhiteSpace(Type) || From.HasValue || To.HasValue;
}

public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public HashSet<int> EnrolledCategoryIds { get; set; } = new();
    public bool IsPast => Event.EventDate.Date < DateTime.Today;
}

public class ProfileFormViewModel
{
    [Required(ErrorMessage = "Enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [RegularExpression(@"^[0-9+\s()\-]{7,20}$", ErrorMessage = "Enter a valid phone number.")]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }
}

public class ProfilePageViewModel
{
    public UserDto User { get; set; } = new();
    public ProfileFormViewModel Form { get; set; } = new();
}
