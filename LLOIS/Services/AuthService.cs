namespace LLOIS.Services;

public interface IAuthService
{
    Task<ApiUser> LoginAsync(string username, string password, bool rememberMe = false);
    void Logout();
    Task<IEnumerable<ApiUserSummary>> GetAllUsersAsync();
    Task CreateUserAsync(string username, string password, int role);
    Task UpdateUserAsync(int userId, string newUsername, int newRole);
    Task ResetPasswordAsync(int userId, string newPassword);
    Task SetActiveStatusAsync(int userId, bool isActive);
    Task<IEnumerable<ApiAuditLog>> GetRecentLogsAsync(int count = 200);
}

public enum ApiUserRole { Viewer = 0, Encoder = 1, Admin = 2, SuperAdmin = 3 }

public class AuthService(ApiClient api) : IAuthService
{
    public async Task<ApiUser> LoginAsync(string username, string password, bool rememberMe = false)
    {
        var result = await api.LoginAsync(username, password);
        SessionContext.Set(result.User, result.Token, rememberMe);
        return result.User;
    }

    public void Logout()
    {
        SessionContext.Clear();
        api.ClearToken();
    }

    public Task<IEnumerable<ApiUserSummary>> GetAllUsersAsync() =>
        api.GetAsync<IEnumerable<ApiUserSummary>>("api/users")!;

    public Task CreateUserAsync(string username, string password, int role) =>
        api.PostAsync<object>("api/users", new { username, password, role });

    public Task UpdateUserAsync(int userId, string newUsername, int newRole) =>
        api.PostAsync<object>($"api/users/{userId}?_method=PUT", new { username = newUsername, role = newRole });

    public Task ResetPasswordAsync(int userId, string newPassword) =>
        api.PostAsync<object>($"api/users/{userId}/password?_method=PUT", new { password = newPassword });

    public Task SetActiveStatusAsync(int userId, bool isActive) =>
        api.PostAsync<object>($"api/users/{userId}/status?_method=PUT", new { is_active = isActive });

    public async Task<IEnumerable<ApiAuditLog>> GetRecentLogsAsync(int count = 200)
    {
        var result = await api.GetAsync<IEnumerable<ApiAuditLog>>($"api/audit-logs?count={count}");
        return result ?? Enumerable.Empty<ApiAuditLog>();
    }
}

public class ApiAuditLog
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? Source { get; set; }
    public DateTime? CreatedAt { get; set; }   // nullable: rows created by the old .NET app may have no created_at

    // Laravel sends created_at in UTC ("...Z"); show Philippine time (UTC+8), like the old model.
    public DateTime? TimestampPh => CreatedAt?.ToUniversalTime().AddHours(8);
    public string TimestampDisplay => TimestampPh?.ToString("MM/dd/yyyy HH:mm:ss") ?? "";
}



public class ApiUserSummary
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public ApiUserRole Role { get; set; }
    public bool IsActive { get; set; }
}