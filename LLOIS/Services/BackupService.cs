namespace LLOIS.Services;

using System.IO;
using System.IO.Compression;
using System.Net.Http;
using ClosedXML.Excel;

public static class BackupService
{
    public static async Task CreateBackupAsync(
        ApiClient api,
        IReportService reportService,
        IAuthService authService,
        string destinationZipPath,
        IProgress<string>? progress = null)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "DLIS_Backup_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        var dataDir  = Path.Combine(tempDir, "data");
        var filesDir = Path.Combine(tempDir, "files");
        Directory.CreateDirectory(dataDir);
        Directory.CreateDirectory(filesDir);

        try
        {
            progress?.Report("Exporting ordinances...");
            var ordinances = await reportService.GetOrdinancesAsync(); // full, unpaginated
            progress?.Report($"Found {ordinances.Count} ordinance(s).");
            ExportOrdinancesToExcel(ordinances, Path.Combine(dataDir, "ordinances.xlsx"));

            progress?.Report("Exporting committee reports...");
            var reports = await GetAllPagesAsync<ApiCommitteeReport>(api, "api/committee-reports");
            progress?.Report($"Found {reports.Count} committee report(s).");
            ExportCommitteeReportsToExcel(reports, Path.Combine(dataDir, "committee_reports.xlsx"));

            progress?.Report("Exporting resolutions...");
            var resolutions = await GetAllPagesAsync<ApiResolution>(api, "api/resolutions");
            progress?.Report($"Found {resolutions.Count} resolution(s).");
            ExportResolutionsToExcel(resolutions, Path.Combine(dataDir, "resolutions.xlsx"));

            progress?.Report("Exporting users...");
            var users = (await authService.GetAllUsersAsync()).ToList();
            ExportUsersToExcel(users, Path.Combine(dataDir, "users.xlsx"));

            progress?.Report("Exporting audit log...");
            var auditLogs = (await authService.GetRecentLogsAsync(500)).ToList(); // server caps at 500
            ExportAuditLogsToExcel(auditLogs, Path.Combine(dataDir, "audit_logs.xlsx"));

            // ── Ordinance documents ──
            progress?.Report("Downloading ordinance PDFs...");
            var ordinancePdfDir = Path.Combine(filesDir, "OrdinancePdfs");
            Directory.CreateDirectory(ordinancePdfDir);

            int pdfCount = 0;
            foreach (var o in ordinances.Where(o => !string.IsNullOrEmpty(o.DocumentPath)))
            {
                try
                {
                    var fileName = GetSafeFileName(o.DocumentPath!, o.OrdinanceNumber, "pdf");
                    await DownloadFileAsync(o.DocumentPath!, Path.Combine(ordinancePdfDir, fileName));
                    pdfCount++;
                }
                catch { /* skip files that fail to download */ }
            }
            progress?.Report($"Downloaded {pdfCount} ordinance PDF(s).");

            // ── Committee report attachments (need each report's details for the file list) ──
            progress?.Report("Downloading committee report files...");
            var committeeFilesDir = Path.Combine(filesDir, "CommitteeReportFiles");
            Directory.CreateDirectory(committeeFilesDir);

            int attachmentCount = 0;
            foreach (var r in reports.Where(r => r.AttachmentsCount > 0))
            {
                try
                {
                    var details = await api.GetAsync<ApiCommitteeReportDetails>($"api/committee-reports/{r.Id}");
                    if (details is null) continue;

                    foreach (var a in details.Attachments.Where(a => !string.IsNullOrEmpty(a.FilePath)))
                    {
                        try
                        {
                            var fileName = GetSafeFileName(a.FilePath, $"{r.ReportNumber}_{a.FileName}", null);
                            await DownloadFileAsync(a.FilePath, Path.Combine(committeeFilesDir, fileName));
                            attachmentCount++;
                        }
                        catch { /* skip files that fail to download */ }
                    }
                }
                catch (UnauthorizedAccessException) { throw; } // expired session should still surface
                catch { /* skip this report's attachments */ }
            }
            progress?.Report($"Downloaded {attachmentCount} committee report file(s).");

            // ── Resolution documents ──
            progress?.Report("Downloading resolution files...");
            var resolutionFilesDir = Path.Combine(filesDir, "ResolutionFiles");
            Directory.CreateDirectory(resolutionFilesDir);

            int resolutionFileCount = 0;
            foreach (var r in resolutions.Where(r => !string.IsNullOrEmpty(r.DocumentPath)))
            {
                try
                {
                    var fileName = GetSafeFileName(r.DocumentPath!, r.ResolutionNumber, "pdf");
                    await DownloadFileAsync(r.DocumentPath!, Path.Combine(resolutionFilesDir, fileName));
                    resolutionFileCount++;
                }
                catch { /* skip files that fail to download */ }
            }
            progress?.Report($"Downloaded {resolutionFileCount} resolution file(s).");

            var manifestPath = Path.Combine(tempDir, "manifest.txt");
            await File.WriteAllTextAsync(manifestPath,
                $"DLIS Backup\n" +
                $"Created: {DateTime.UtcNow:u}\n" +
                $"App Version: {App.CurrentVersion}\n" +
                $"Ordinances: {ordinances.Count}\n" +
                $"Committee Reports: {reports.Count}\n" +
                $"Resolutions: {resolutions.Count}\n" +
                $"Users: {users.Count}\n" +
                $"Audit Log Entries: {auditLogs.Count}\n" +
                $"Ordinance PDFs Downloaded: {pdfCount}\n" +
                $"Committee Report Files Downloaded: {attachmentCount}\n" +
                $"Resolution Files Downloaded: {resolutionFileCount}\n");

            progress?.Report("Compressing backup...");
            if (File.Exists(destinationZipPath)) File.Delete(destinationZipPath);
            ZipFile.CreateFromDirectory(tempDir, destinationZipPath, CompressionLevel.Optimal, false);

            progress?.Report("Backup complete.");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best-effort cleanup */ }
        }
    }

    // The list endpoints return 20 per page, so walk every page.
    private static async Task<List<T>> GetAllPagesAsync<T>(ApiClient api, string endpoint)
    {
        var all = new List<T>();
        var page = 1;
        var lastPage = 1;

        do
        {
            var result = await api.GetAsync<PagedResult<T>>($"{endpoint}?page={page}");
            if (result is null) break;

            all.AddRange(result.Data);
            lastPage = result.LastPage;
            page++;
        } while (page <= lastPage);

        return all;
    }

    // ── Excel exports ───────────────────────────────────────────

    private static void ExportOrdinancesToExcel(List<ApiOrdinance> ordinances, string path)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Ordinances");

        string[] headers = ["Ordinance Number", "Series", "Title", "Subject", "Type", "Status",
                             "Sponsor", "Committee", "Date Passed", "Date Approved", "Date Published",
                             "Document Path", "Reference Number", "Location", "Version Count"];
        WriteHeaders(ws, headers);

        int row = 2;
        foreach (var o in ordinances)
        {
            ws.Cell(row, 1).Value  = o.OrdinanceNumber;
            ws.Cell(row, 2).Value  = o.SeriesNumber ?? "";
            ws.Cell(row, 3).Value  = o.Title;
            ws.Cell(row, 4).Value  = o.Subject ?? "";
            ws.Cell(row, 5).Value  = o.Type;
            ws.Cell(row, 6).Value  = o.Status;
            ws.Cell(row, 7).Value  = o.Sponsor ?? "";
            ws.Cell(row, 8).Value  = o.Committee ?? "";
            ws.Cell(row, 9).Value  = o.DatePassed?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(row, 10).Value = o.DateApproved?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(row, 11).Value = o.DatePublished?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(row, 12).Value = o.DocumentPath ?? "";
            ws.Cell(row, 13).Value = o.ReferenceNumber ?? "";
            ws.Cell(row, 14).Value = o.Location ?? "";
            ws.Cell(row, 15).Value = o.Versions.Count;
            row++;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(path);
    }

    private static void ExportCommitteeReportsToExcel(List<ApiCommitteeReport> reports, string path)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("CommitteeReports");

        string[] headers = ["Report Number", "Date", "Submitted By", "Sponsored By", "Subject", "Attachment Count"];
        WriteHeaders(ws, headers);

        int row = 2;
        foreach (var r in reports)
        {
            ws.Cell(row, 1).Value = r.ReportNumber;
            ws.Cell(row, 2).Value = r.Date?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(row, 3).Value = r.SubmittedBy ?? "";
            ws.Cell(row, 4).Value = r.SponsoredBy ?? "";
            ws.Cell(row, 5).Value = r.Subject ?? "";
            ws.Cell(row, 6).Value = r.AttachmentsCount;
            row++;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(path);
    }

    private static void ExportResolutionsToExcel(List<ApiResolution> resolutions, string path)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Resolutions");

        string[] headers = ["Resolution Number", "SB Term", "Session Info", "Committee",
                             "Title", "Sponsor", "Date Approved", "Document Path"];
        WriteHeaders(ws, headers);

        int row = 2;
        foreach (var r in resolutions)
        {
            ws.Cell(row, 1).Value = r.ResolutionNumber;
            ws.Cell(row, 2).Value = r.SbTerm ?? "";
            ws.Cell(row, 3).Value = r.SessionInfo ?? "";
            ws.Cell(row, 4).Value = r.Committee ?? "";
            ws.Cell(row, 5).Value = r.Title;
            ws.Cell(row, 6).Value = r.Sponsor ?? "";
            ws.Cell(row, 7).Value = r.DateApproved?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(row, 8).Value = r.DocumentPath ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(path);
    }

    private static void ExportUsersToExcel(List<ApiUserSummary> users, string path)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Users");

        WriteHeaders(ws, ["Username", "Role", "Active"]);

        int row = 2;
        foreach (var u in users)
        {
            ws.Cell(row, 1).Value = u.Username;
            ws.Cell(row, 2).Value = u.Role.ToString();
            ws.Cell(row, 3).Value = u.IsActive ? "Yes" : "No";
            row++;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(path);
    }

    private static void ExportAuditLogsToExcel(List<ApiAuditLog> logs, string path)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("AuditLogs");

        WriteHeaders(ws, ["Timestamp", "User", "Source", "Action", "Details"]);

        int row = 2;
        foreach (var log in logs)
        {
            ws.Cell(row, 1).Value = log.TimestampDisplay;
            ws.Cell(row, 2).Value = log.Username;
            ws.Cell(row, 3).Value = log.Source ?? "";
            ws.Cell(row, 4).Value = log.Action;
            ws.Cell(row, 5).Value = log.Details ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(path);
    }

    private static void WriteHeaders(IXLWorksheet ws, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }
    }

    // ── File helpers (unchanged) ────────────────────────────────

    private static string GetSafeFileName(string url, string fallbackBaseName, string? fallbackExtension)
    {
        if (url.Contains("drive.google.com"))
        {
            var ext = string.IsNullOrEmpty(fallbackExtension) ? "" : $".{fallbackExtension}";
            return SanitizeFileName($"{fallbackBaseName}{ext}");
        }

        return Path.GetFileName(new Uri(url).LocalPath);
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    private static async Task DownloadFileAsync(string publicUrl, string destinationPath)
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
        await File.WriteAllBytesAsync(destinationPath, bytes);
    }

    private static string? ExtractDriveFileId(string driveUrl)
    {
        var match = System.Text.RegularExpressions.Regex.Match(driveUrl, @"/d/([a-zA-Z0-9_-]+)");
        if (match.Success) return match.Groups[1].Value;

        match = System.Text.RegularExpressions.Regex.Match(driveUrl, @"[?&]id=([a-zA-Z0-9_-]+)");
        return match.Success ? match.Groups[1].Value : null;
    }
}