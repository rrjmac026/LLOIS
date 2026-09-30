namespace LLOIS.Services;

using System.IO;
using System.Text.Json;

public static class SessionContext
{
    private static readonly string TokenPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DLIS", "session.json");

    public static ApiUser? CurrentUser { get; private set; }
    public static string? Token { get; private set; }

    public static void Set(ApiUser user, string token, bool remember)
    {
        CurrentUser = user;
        Token = token;

        if (remember)
            Persist(user, token);
    }

    public static void Clear()
    {
        CurrentUser = null;
        Token = null;

        try { if (File.Exists(TokenPath)) File.Delete(TokenPath); }
        catch { /* best-effort */ }
    }

    /// <summary>
    /// Attempts to restore a previous session on app startup. Returns true if
    /// a token was found and loaded (caller should still validate it against
    /// /api/me before trusting it — a token may have been revoked server-side).
    /// </summary>
    public static bool TryRestore(out string? token)
    {
        token = null;

        try
        {
            if (!File.Exists(TokenPath)) return false;

            var json = File.ReadAllText(TokenPath);
            var data = JsonSerializer.Deserialize<PersistedSession>(json);
            if (data is null || string.IsNullOrEmpty(data.Token)) return false;

            token = data.Token;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void Persist(ApiUser user, string token)
    {
        try
        {
            var dir = Path.GetDirectoryName(TokenPath)!;
            Directory.CreateDirectory(dir);

            var data = new PersistedSession { Token = token, Username = user.Username };
            File.WriteAllText(TokenPath, JsonSerializer.Serialize(data));
        }
        catch { /* non-critical — user just has to log in again next time */ }
    }

    private class PersistedSession
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }
}