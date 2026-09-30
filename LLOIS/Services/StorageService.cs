namespace LLOIS.Services;

using System.IO;
using System.Net.Http;

public static class StorageService
{
    /// <summary>
    /// Downloads a file from a public URL (Google Drive share link or Supabase
    /// public URL) to a local destination path.
    /// Used by BackupService to pull Ordinance PDFs and Committee Report files into a ZIP.
    /// </summary>
    public static async Task DownloadFileAsync(string publicUrl, string destinationPath)
    {
        using var client = new HttpClient();

        var actualDownloadUrl = publicUrl;

        if (publicUrl.Contains("drive.google.com"))
        {
            var fileId = ExtractDriveFileId(publicUrl);
            if (fileId is not null)
                actualDownloadUrl = $"https://drive.google.com/uc?export=download&id={fileId}";
        }

        var response = await client.GetAsync(actualDownloadUrl);
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync();
        await System.IO.File.WriteAllBytesAsync(destinationPath, bytes);
    }

    private static string? ExtractDriveFileId(string driveUrl)
    {
        var match = System.Text.RegularExpressions.Regex.Match(driveUrl, @"/d/([a-zA-Z0-9_-]+)");
        if (match.Success) return match.Groups[1].Value;

        match = System.Text.RegularExpressions.Regex.Match(driveUrl, @"[?&]id=([a-zA-Z0-9_-]+)");
        return match.Success ? match.Groups[1].Value : null;
    }
}