namespace LLOIS.Services;

using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

public static class UpdateService
{
    private const string LatestReleaseUrl =
        "https://api.github.com/repos/rrjmac026/LLOIS-releases/releases/latest";

    public class GitHubAsset
    {
        public string Name { get; set; } = "";
        public string Browser_Download_Url { get; set; } = "";
    }

    public class GitHubRelease
    {
        public string Tag_Name { get; set; } = "";
        public List<GitHubAsset> Assets { get; set; } = new();
    }

    public class UpdateInfo
    {
        public string Version { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string FileName { get; set; } = "";
    }

    public static async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("LLOIS-Updater", "1.0"));

        var json = await client.GetStringAsync(LatestReleaseUrl);
        var release = JsonSerializer.Deserialize<GitHubRelease>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (release is null || release.Assets.Count == 0) return null;

        var remoteVersion = release.Tag_Name.TrimStart('v');
        if (!IsNewer(remoteVersion, App.CurrentVersion)) return null;

        var exeAsset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".exe"));
        if (exeAsset is null) return null;

        return new UpdateInfo
        {
            Version = remoteVersion,
            DownloadUrl = exeAsset.Browser_Download_Url,
            FileName = exeAsset.Name
        };
    }

    private static bool IsNewer(string remote, string current)
    {
        var r = new Version(remote);
        var c = new Version(current);
        return r > c;
    }

    public static async Task<string> DownloadUpdateAsync(UpdateInfo info, IProgress<double>? progress = null)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), info.FileName);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("LLOIS-Updater", "1.0"));

        using var response = await client.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[81920];
        long totalRead = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read));
            totalRead += read;
            if (totalBytes > 0)
                progress?.Report((double)totalRead / totalBytes * 100);
        }

        return tempPath;
    }

    public static void ApplyUpdateAndRestart(string newExePath)
    {
        var currentExePath = Environment.ProcessPath;

        if (string.IsNullOrEmpty(currentExePath) || string.IsNullOrEmpty(newExePath))
        {
            System.Windows.MessageBox.Show(
                $"Update failed — could not determine file paths.\nCurrent: {currentExePath}\nNew: {newExePath}",
                "Update Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }

        var updaterPath = Path.Combine(AppContext.BaseDirectory, "LLOIS.Updater.exe");

        if (!File.Exists(updaterPath))
        {
            System.Windows.MessageBox.Show(
                $"Updater not found at: {updaterPath}",
                "Update Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }

        // Write a marker so the next launch knows an update just happened
        try
        {
            var markerPath = Path.Combine(Path.GetTempPath(), "LLOIS_update_marker.txt");
            File.WriteAllText(markerPath, App.CurrentVersion);
        }
        catch { /* non-critical */ }

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = updaterPath,
            Arguments = $"\"{newExePath}\" \"{currentExePath}\"",
            UseShellExecute = true,
            Verb = "runas"   // request elevation — install dir needs admin rights to write
        };

        try
        {
            System.Diagnostics.Process.Start(psi);
            System.Windows.Application.Current.Shutdown();
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // User clicked "No" on the UAC prompt
            System.Windows.MessageBox.Show(
                "The update was cancelled because administrator permission was not granted.\n\nPlease try again and allow the prompt to continue.",
                "Update Cancelled", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Failed to start the updater:\n{ex.Message}",
                "Update Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    public static string? ConsumeUpdateMarker()
    {
        try
        {
            var markerPath = Path.Combine(Path.GetTempPath(), "LLOIS_update_marker.txt");
            if (!File.Exists(markerPath)) return null;

            var previousVersion = File.ReadAllText(markerPath);
            File.Delete(markerPath);
            return previousVersion;
        }
        catch
        {
            return null;
        }
    }
}