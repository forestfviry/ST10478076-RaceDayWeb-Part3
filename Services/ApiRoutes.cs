namespace RaceDayWeb.Services;

/// <summary>Every API route the MVC app calls lives here.</summary>
public static class ApiRoutes
{
    // Auth (AuthController was not part of the files shared, so these two are assumed)
    public const string Register = "api/auth/register";
    public const string Login = "api/auth/login";

    // Events
    public const string Events = "api/events";                      // public, upcoming only
    public const string MyEvents = "api/events/mine";               // organiser, all own events (API addition)
    public static string Event(int id) => $"api/events/{id}";
    public static string EventBanner(int id) => $"api/events/{id}/banner";               // API addition
    public static string EventEnrolments(int id) => $"api/events/{id}/enrolments";       // API addition
    public static string EventResults(int id) => $"api/events/{id}/results";             // API addition

    // Categories
    public static string EventCategories(int eventId) => $"api/events/{eventId}/categories";
    public static string Category(int id) => $"api/categories/{id}";

    // Enrolments
    public static string Enrol(int categoryId) => $"api/categories/{categoryId}/enrol";
    public const string MyEnrolments = "api/users/me/enrolments";
    public static string CancelEnrolment(int id) => $"api/enrolments/{id}";

    // Results
    public static string EnrolmentResult(int enrolmentId) => $"api/enrolments/{enrolmentId}/result";
    public static string Result(int id) => $"api/results/{id}";
    public const string MyResults = "api/users/me/results";         // API addition

    // Profile
    public const string Me = "api/users/me";
    public const string MePicture = "api/users/me/picture";         // API addition
}
