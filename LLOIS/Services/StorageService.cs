namespace LLOIS.Services;

using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using DotNetEnv;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

public static class StorageService
{
    private const string OrdinanceBucket = "ordinance-pdfs";
    private const string CommitteeReportBucket = "committee-report-files"; // create this bucket in Supabase

    private const string ResolutionBucket = "resolution-files";
    private const string MinutesBucket = "minutes-files";

    private const string OrdinanceDriveFolder = "Ordinances";
    private const string CommitteeReportDriveFolder = "Committee Reports";
    private const string ResolutionDriveFolder = "Resolutions";
    private const string MinutesDriveFolder = "Minutes";

    private static DriveService? _driveService;
    private static readonly object _driveLock = new();
    private static readonly string[] Scopes = { DriveService.Scope.Drive };

    // ── Drive client (lazy singleton) ───────────────────────────
    private static DriveService GetDriveService()
    {
        if (_driveService is not null) return _driveService;

        lock (_driveLock)
        {
            if (_driveService is not null) return _driveService;

            Env.Load();
            var clientJsonPath = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_JSON")
                ?? throw new InvalidOperationException("GOOGLE_OAUTH_CLIENT_JSON is not set in .env");

            UserCredential credential;
            using (var stream = new FileStream(clientJsonPath, FileMode.Open, FileAccess.Read))
            {
                var tokenStorePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LLOIS", "google-drive-token");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(tokenStorePath, true)
                ).GetAwaiter().GetResult();
            }

            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "LLOIS"
            });

            return _driveService;
        }
    }

    // ── finds (or creates) a named subfolder under the shared root ──
    private static string GetOrCreateSubfolder(DriveService drive, string folderName)
    {
        var rootId = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_ROOT_FOLDER_ID")
            ?? throw new InvalidOperationException("GOOGLE_DRIVE_ROOT_FOLDER_ID is not set in .env");

        var listRequest = drive.Files.List();
        listRequest.Q = $"'{rootId}' in parents and name = '{folderName}' " +
                        "and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
        listRequest.Fields = "files(id, name)";
        listRequest.Spaces = "drive";

        var existing = listRequest.Execute().Files.FirstOrDefault();
        if (existing is not null) return existing.Id;

        var folderMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = folderName,
            MimeType = "application/vnd.google-apps.folder",
            Parents = new[] { rootId }
        };
        var createRequest = drive.Files.Create(folderMetadata);
        createRequest.Fields = "id";
        return createRequest.Execute().Id;
    }

    /// <summary>
    /// Uploads a local file to a named Drive subfolder, sets it to
    /// "anyone with the link can view", and returns a usable URL.
    /// </summary>
    public static string UploadFileToDrive(string localFilePath, string folderName)
    {
        var drive = GetDriveService();
        var parentId = GetOrCreateSubfolder(drive, folderName);

        var fileMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = $"{Guid.NewGuid()}_{Path.GetFileName(localFilePath)}",
            Parents = new[] { parentId }
        };

        byte[] fileBytes = ReadFileWithRetry(localFilePath);
        using var stream = new MemoryStream(fileBytes);

        var request = drive.Files.Create(fileMetadata, stream, GetContentType(localFilePath));
        request.Fields = "id, webViewLink, webContentLink";

        var progress = request.Upload();

        if (progress.Status != Google.Apis.Upload.UploadStatus.Completed)
        {
            var reason = progress.Exception?.Message ?? progress.Status.ToString();
            throw new InvalidOperationException($"Drive upload did not complete: {reason}");
        }

        var uploaded = request.ResponseBody
            ?? throw new InvalidOperationException("Drive upload completed but returned no metadata.");

        // Make it publicly viewable via link
        var permission = new Permission { Type = "anyone", Role = "reader" };
        drive.Permissions.Create(permission, uploaded.Id).Execute();

        // webViewLink opens Google's viewer; webContentLink triggers a direct download
        return uploaded.WebViewLink ?? $"https://drive.google.com/file/d/{uploaded.Id}/view";
    }

    // ── updated public methods — same signatures, now call Drive ──
    public static string UploadMinutesFile(string localFilePath) =>
        UploadFileToDrive(localFilePath, MinutesDriveFolder);

    public static string UploadResolutionFile(string localFilePath) =>
        UploadFileToDrive(localFilePath, ResolutionDriveFolder);

    public static string UploadPdf(string localFilePath) =>
        UploadFileToDrive(localFilePath, OrdinanceDriveFolder);

    public static string UploadCommitteeReportFile(string localFilePath) =>
        UploadFileToDrive(localFilePath, CommitteeReportDriveFolder);

    /// <summary>
    /// Generic upload — any file, any bucket, any content type.
    /// </summary>
    public static string UploadFile(string localFilePath, string bucketName, string contentType)
    {
        Env.Load();

        var projectUrl = Environment.GetEnvironmentVariable("SUPABASE_URL")
            ?? throw new InvalidOperationException("SUPABASE_URL is not set in .env");
        var serviceKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_KEY")
            ?? throw new InvalidOperationException("SUPABASE_SERVICE_KEY is not set in .env");

        var rawFileName = $"{Guid.NewGuid()}_{Path.GetFileName(localFilePath)}";
        var safeFileName = Uri.EscapeDataString(rawFileName);
        var uploadUrl = $"{projectUrl}/storage/v1/object/{bucketName}/{safeFileName}";

        byte[] fileBytes = ReadFileWithRetry(localFilePath);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", serviceKey);

        using var content = new ByteArrayContent(fileBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var response = client.PostAsync(uploadUrl, content).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            throw new InvalidOperationException($"Upload failed ({(int)response.StatusCode}): {errorBody}");
        }

        return $"{projectUrl}/storage/v1/object/public/{bucketName}/{safeFileName}";
    }

    private static byte[] ReadFileWithRetry(string path, int maxAttempts = 5)
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var ms = new MemoryStream();
                fs.CopyTo(ms);
                return ms.ToArray();
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                Thread.Sleep(300); // brief pause before retry — file may be transiently locked
            }
        }

        // Final attempt without catching, so the real exception surfaces if it still fails
        using var finalFs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var finalMs = new MemoryStream();
        finalFs.CopyTo(finalMs);
        return finalMs.ToArray();
    }

    /// <summary>
    /// Downloads a file from a public Supabase Storage URL to a local destination path.
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

    private static string GetContentType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".pdf"  => "application/pdf",
            ".doc"  => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls"  => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".png"  => "image/png",
            ".jpg"  => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".txt"  => "text/plain",
            ".zip"  => "application/zip",
            _       => "application/octet-stream"
        };
    }
}