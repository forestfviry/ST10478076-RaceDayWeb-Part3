using System.Text;
using System.Text.Json;

namespace RaceDayWeb.Helpers;

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public static class SessionKeys
{
    public const string Token = "token";
    public const string Role = "role";
    public const string UserName = "userName";
    public const string UserId = "userId";
    public const string ExpiresUtc = "expiresUtc";
}

public record TokenInfo(int UserId, string Name, string Role, DateTime ExpiresUtc);

public static class SessionExtensions
{
    public static bool IsLoggedIn(this ISession s) => !string.IsNullOrEmpty(s.GetString(SessionKeys.Token));
    public static string? GetRole(this ISession s) => s.GetString(SessionKeys.Role);
    public static string GetUserName(this ISession s) => s.GetString(SessionKeys.UserName) ?? "";

    public static bool IsInRole(this ISession s, string role) =>
        string.Equals(s.GetRole(), role, StringComparison.OrdinalIgnoreCase);

    public static bool IsExpired(this ISession s) =>
        long.TryParse(s.GetString(SessionKeys.ExpiresUtc), out var ticks) && new DateTime(ticks, DateTimeKind.Utc) <= DateTime.UtcNow;

    public static void SignIn(this ISession s, string token, TokenInfo info)
    {
        s.SetString(SessionKeys.Token, token);
        s.SetString(SessionKeys.Role, info.Role);
        s.SetString(SessionKeys.UserName, info.Name);
        s.SetString(SessionKeys.UserId, info.UserId.ToString());
        s.SetString(SessionKeys.ExpiresUtc, info.ExpiresUtc.Ticks.ToString());
    }
}

/// <summary>Reads the role, name and expiry out of the API's JWT. The signature is not checked here, the API does that on every request.</summary>
public static class TokenReader
{
    private static readonly string[] RoleClaims = { "role", "roles", "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" };
    private static readonly string[] NameClaims = { "unique_name", "name", "given_name", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "email" };
    private static readonly string[] IdClaims = { "nameid", "sub", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" };

    public static TokenInfo? Read(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2) return null;

        try
        {
            using var doc = JsonDocument.Parse(Base64UrlDecode(parts[1]));
            var root = doc.RootElement;

            var role = Find(root, RoleClaims) ?? "";

            var name = Find(root, NameClaims) ?? "User";
            int.TryParse(Find(root, IdClaims), out var userId);

            var expires = DateTime.MaxValue;
            if (root.TryGetProperty("exp", out var exp) && exp.TryGetInt64(out var seconds))
                expires = DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;

            return new TokenInfo(userId, name, role, expires);
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            return null;
        }
    }

    private static string? Find(JsonElement root, string[] names)
    {
        foreach (var n in names)
        {
            if (!root.TryGetProperty(n, out var v)) continue;
            if (v.ValueKind == JsonValueKind.Array)
                return v.EnumerateArray().Select(x => x.ToString()).FirstOrDefault();
            return v.ToString();
        }
        return null;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
        return Convert.FromBase64String(s);
    }
}
