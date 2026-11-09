using System.Globalization;

namespace RaceDayWeb.Helpers;

public static class Display
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>Colour-codes an enrolment status: green for confirmed, amber while it is still pending.</summary>
    public static string StatusClass(string? status) => (status ?? "").Trim().ToLowerInvariant() switch
    {
        "confirmed" or "paid" or "approved" => "status-good",
        "registered" or "pending" => "status-warn",
        "cancelled" or "rejected" => "status-bad",
        _ => "status-muted"
    };

    public static string Date(DateTime d) => d.ToString("d MMM yyyy", Inv);
    public static string Money(decimal amount) => amount <= 0 ? "Free" : "R" + amount.ToString("0.00", Inv);
    public static string Km(decimal km) => km.ToString("0.##", Inv) + " km";

    public static string Ordinal(int n)
    {
        if (n % 100 is >= 11 and <= 13) return $"{n}th";
        return (n % 10) switch { 1 => $"{n}st", 2 => $"{n}nd", 3 => $"{n}rd", _ => $"{n}th" };
    }
}

public static class ImageRules
{
    private static readonly string[] Allowed = { "image/jpeg", "image/png", "image/webp" };
    private const long MaxBytes = 5 * 1024 * 1024;

    /// <summary>Returns an error message, or null when the file is fine (or absent).</summary>
    public static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0) return null;
        if (!Allowed.Contains(file.ContentType)) return "Upload a JPG, PNG or WebP image.";
        if (file.Length > MaxBytes) return "The image must be 5 MB or smaller.";
        return null;
    }
}
