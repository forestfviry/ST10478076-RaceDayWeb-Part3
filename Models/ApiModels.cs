using System.Text.Json.Serialization;

namespace RaceDayWeb.Models;

// These classes mirror the JSON the RaceDay API sends and receives.

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RegisterRequest
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = "";   // "Organiser" or "Participant"
}

public class AuthUser
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
}

public class AuthResponse
{
    public string? Token { get; set; }
    public string? AccessToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public AuthUser? User { get; set; }

    [JsonIgnore]
    public string? ResolvedToken => Token ?? AccessToken;
}

public class EventDto
{
    public int EventID { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public string? Description { get; set; }
    public int OrganiserID { get; set; }
    public string OrganiserName { get; set; } = "";
    public string? BannerUrl { get; set; }
    public string? EventType { get; set; }
    public decimal? DistanceKm { get; set; }
}

public class EventRequest
{
    public string EventName { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = "";
    public string? Description { get; set; }
    public string? EventType { get; set; }
    public decimal? DistanceKm { get; set; }
}

public class CategoryDto
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
}

public class CategoryRequest
{
    public string CategoryName { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
}

public class EnrolmentDto
{
    public int EnrolmentID { get; set; }
    public int UserID { get; set; }
    public string ParticipantName { get; set; } = "";
    public string? ParticipantEmail { get; set; }
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = "";
    public string EventName { get; set; } = "";
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = "";
    public int EventID { get; set; }
    public DateTime? EventDate { get; set; }
    public string? EventLocation { get; set; }
}

public class ResultDto
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = "";
    public string ParticipantName { get; set; } = "";
}

public class ResultRequest
{
    public string FinishTime { get; set; } = "";
    public int? Position { get; set; }
    public string Status { get; set; } = "Finished";
}

public class MyResultDto
{
    public int ResultID { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = "";
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
    public int TotalFinishers { get; set; }
    public string Status { get; set; } = "";
}

public class UserDto
{
    public int UserID { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class UpdateProfileRequest
{
    public string FullName { get; set; } = "";
    public string? PhoneNumber { get; set; }
}
