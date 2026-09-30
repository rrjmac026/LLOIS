namespace LLOIS.Services;

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

public class ApiClient
{
    private readonly HttpClient _http;
    private string? _token;

    // Laravel returns snake_case JSON (ordinance_number, role_name, added_by...)
    // while our DTOs are PascalCase (OrdinanceNumber, RoleName, AddedBy...).
    // Without this, System.Text.Json can't match them and every such field
    // silently comes back null/default.
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public void SetToken(string token)
    {
        _token = token;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearToken()
    {
        _token = null;
        _http.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        var response = await _http.PostAsJsonAsync("api/login", new { username, password }, JsonOptions);

        if (!response.IsSuccessStatusCode)
        {
            var message = await ReadErrorMessageAsync(response);

            // Bad credentials / inactive account — LoginView shows these inline in its banner.
            if (response.StatusCode is HttpStatusCode.UnprocessableEntity or HttpStatusCode.Unauthorized)
                throw new InvalidOperationException(message);

            throw new ApiException(response.StatusCode, message);
        }

        var result = await ReadAsync<LoginResult>(response)
            ?? throw new InvalidOperationException("Unexpected login response.");

        SetToken(result.Token);
        return result;
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        var response = await _http.GetAsync(endpoint);
        await EnsureOkAsync(response);
        return await ReadAsync<T>(response);
    }

    public async Task<T?> PostAsync<T>(string endpoint, object payload)
    {
        var response = await _http.PostAsJsonAsync(endpoint, payload, JsonOptions);
        await EnsureOkAsync(response);
        return await ReadAsync<T>(response);
    }

    public async Task<T?> PostMultipartAsync<T>(string endpoint, MultipartFormDataContent content)
    {
        var response = await _http.PostAsync(endpoint, content);
        await EnsureOkAsync(response);
        return await ReadAsync<T>(response);
    }

    public async Task DeleteAsync(string endpoint)
    {
        var response = await _http.DeleteAsync(endpoint);
        await EnsureOkAsync(response);
    }

    // ── Response handling ───────────────────────────────────────

    private static async Task EnsureOkAsync(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Session expired. Please log in again.");

        if (response.IsSuccessStatusCode) return;

        throw new ApiException(response.StatusCode, await ReadErrorMessageAsync(response));
    }

    // Tolerates empty bodies (204 etc.) instead of throwing a JsonException.
    private static async Task<T?> ReadAsync<T>(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
            return default;

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    // Pulls Laravel's own message out of the error body so the user sees
    // "The message field is required." instead of a bare status code.
    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                // Validation: { "message": "...", "errors": { "field": ["msg", ...] } }
                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var lines = errors.EnumerateObject()
                        .Where(p => p.Value.ValueKind == JsonValueKind.Array)
                        .SelectMany(p => p.Value.EnumerateArray())
                        .Where(v => v.ValueKind == JsonValueKind.String)
                        .Select(v => v.GetString())
                        .Where(s => !string.IsNullOrWhiteSpace(s));

                    var joined = string.Join("\n", lines);
                    if (joined.Length > 0) return joined;
                }

                if (root.TryGetProperty("message", out var msg)
                    && msg.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(msg.GetString()))
                    return msg.GetString()!;
            }
        }
        catch (JsonException) { /* not JSON (e.g. an HTML error page) — fall through */ }

        return response.StatusCode switch
        {
            HttpStatusCode.Forbidden => "You don't have permission to do that.",
            HttpStatusCode.NotFound  => "The requested item was not found.",
            _ => $"The server returned an error ({(int)response.StatusCode})."
        };
    }
}

/// <summary>
/// The server answered but refused (403, 404, 422, 500...). Deliberately NOT an
/// HttpRequestException, so ConnectionFailureHandler doesn't mistake a server-side
/// error for a lost connection and bounce the user to the login screen.
/// </summary>
public class ApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public class LoginResult
{
    public string Token { get; set; } = string.Empty;
    public ApiUser User { get; set; } = new();
}

public class ApiUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public int Role { get; set; }
    public string RoleName { get; set; } = string.Empty;
}

public class PagedResult<T>
{
    public List<T> Data { get; set; } = new();
    public int Total { get; set; }
    public int CurrentPage { get; set; }
    public int LastPage { get; set; }
}