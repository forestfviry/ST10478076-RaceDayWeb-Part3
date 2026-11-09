using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RaceDayWeb.Helpers;

namespace RaceDayWeb.Services;

public class ApiResult
{
    public bool Success { get; init; }
    public HttpStatusCode StatusCode { get; init; }
    public string? Error { get; init; }
    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;
    public bool IsForbidden => StatusCode == HttpStatusCode.Forbidden;
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }
}

public interface IApiClient
{
    Task<ApiResult<T>> GetAsync<T>(string url);
    Task<ApiResult<T>> PostAsync<T>(string url, object body);
    Task<ApiResult> PostAsync(string url, object body);
    Task<ApiResult> PutAsync(string url, object body);
    Task<ApiResult> DeleteAsync(string url);
    Task<ApiResult> UploadAsync(string url, IFormFile file);
}

public class ApiClient : IApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<ApiClient> _logger;
    private readonly IHostEnvironment _env;

    public ApiClient(HttpClient client, IHttpContextAccessor http, ILogger<ApiClient> logger, IHostEnvironment env)
    {
        _client = client;
        _http = http;
        _logger = logger;
        _env = env;
    }

    public Task<ApiResult<T>> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url);
    public Task<ApiResult<T>> PostAsync<T>(string url, object body) => SendAsync<T>(HttpMethod.Post, url, ToJson(body));
    public async Task<ApiResult> PostAsync(string url, object body) => await SendAsync<object>(HttpMethod.Post, url, ToJson(body));
    public async Task<ApiResult> PutAsync(string url, object body) => await SendAsync<object>(HttpMethod.Put, url, ToJson(body));
    public async Task<ApiResult> DeleteAsync(string url) => await SendAsync<object>(HttpMethod.Delete, url);

    public async Task<ApiResult> UploadAsync(string url, IFormFile file)
    {
        var form = new MultipartFormDataContent();
        var stream = new StreamContent(file.OpenReadStream());
        stream.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        form.Add(stream, "file", file.FileName);
        return await SendAsync<object>(HttpMethod.Post, url, form);
    }

    private static StringContent ToJson(object body) =>
        new(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };

        var token = _http.HttpContext?.Session.GetString(SessionKeys.Token);
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            using var response = await _client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API {Method} {Url} returned {Status}: {Body}", method, url, (int)response.StatusCode, body);
                var message = ExtractDetail(body) ?? Friendly(response.StatusCode);
                if (_env.IsDevelopment()) message += $" [{method} {url} returned {(int)response.StatusCode}]";
                return new ApiResult<T> { Success = false, StatusCode = response.StatusCode, Error = message };
            }

            T? data = default;
            if (typeof(T) != typeof(object) && !string.IsNullOrWhiteSpace(body))
                data = JsonSerializer.Deserialize<T>(body, Json);

            return new ApiResult<T> { Success = true, StatusCode = response.StatusCode, Data = data };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Could not reach the API for {Method} {Url}", method, url);
            return Fail<T>("RaceDay can't reach its server right now. Please try again in a moment.");
        }
        catch (TaskCanceledException)
        {
            return Fail<T>("The RaceDay API took too long to respond.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Unexpected response shape for {Method} {Url}", method, url);
            return Fail<T>("Something went wrong while reading the response. Please try again.");
        }
    }

    private static string Friendly(HttpStatusCode code) => code switch
    {
        HttpStatusCode.NotFound => "We couldn't find what you were looking for.",
        HttpStatusCode.Conflict => "That clashes with something that already exists.",
        HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => "Some of the details weren't accepted. Check them and try again.",
        HttpStatusCode.Unauthorized => "Please log in again.",
        HttpStatusCode.Forbidden => "You don't have permission to do that.",
        _ => "Something went wrong on our side. Please try again."
    };

    private static ApiResult<T> Fail<T>(string error) =>
        new() { Success = false, StatusCode = HttpStatusCode.ServiceUnavailable, Error = error };

    /// <summary>Pulls a readable message out of the usual ASP.NET error bodies.</summary>
    private static string? ExtractDetail(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.String) return root.GetString();

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var first = errors.EnumerateObject()
                        .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array ? p.Value.EnumerateArray() : Enumerable.Empty<JsonElement>())
                        .Select(e => e.ToString())
                        .FirstOrDefault();
                    if (first is not null) return first;
                }

                foreach (var key in new[] { "message", "error", "detail", "title" })
                    if (root.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
                        return v.GetString();
            }
        }
        catch (JsonException)
        {
            // Not JSON, fall through to plain text.
        }

        var text = body.Trim();
        return text.Length <= 200 && !text.Contains('<') ? text : null;
    }
}
