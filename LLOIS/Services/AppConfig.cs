namespace LLOIS.Services;

using System.IO;
using System.Text.Json;

public static class AppConfig
{
    private static readonly string ConfigPath =
        Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public static string ApiBaseUrl { get; private set; } = "https://dlis-web.onrender.com/";

    public static void Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                // First run — write a default file next to the exe so it's
                // easy to find and edit without recompiling.
                Save();
                return;
            }

            var json = File.ReadAllText(ConfigPath);
            var data = JsonSerializer.Deserialize<ConfigData>(json);

            if (data is not null && !string.IsNullOrWhiteSpace(data.ApiBaseUrl))
                ApiBaseUrl = data.ApiBaseUrl;
        }
        catch
        {
            // Fall back to the default above — config errors shouldn't crash startup.
        }
    }

    private static void Save()
    {
        try
        {
            var data = new ConfigData { ApiBaseUrl = ApiBaseUrl };
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* non-critical */ }
    }

    private class ConfigData
    {
        public string ApiBaseUrl { get; set; } = string.Empty;
    }
}