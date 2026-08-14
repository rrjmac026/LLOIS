namespace LLOIS.Services;

using System.IO;
using System.IO.Compression;
using ClosedXML.Excel;
using LLOIS.Data;
using LLOIS.Models;
using Microsoft.EntityFrameworkCore;

public static class BackupService
{
    public static async Task CreateBackupAsync(SimpleDbContextFactory dbFactory, string destinationZipPath, IProgress<string>? progress = null)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "DLIS_Backup_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        var dataDir  = Path.Combine(tempDir, "data");
        var filesDir = Path.Combine(tempDir, "files");
        Directory.CreateDirectory(dataDir);
        Directory.CreateDirectory(filesDir);

        try
        {
            using var ctx = dbFactory.CreateDbContext();

            progress?.Report("Exporting ordinances...");
            var ordinances = await ctx.Ordinances.Include(o => o.Versions).ToListAsync();
            progress?.Report($"Found {ordinances.Count} ordinance(s).");
            ExportOrdinancesToExcel(ordinances, Path.Combine(dataDir, "ordinances.xlsx"));

            progress?.Report("Exporting committee reports...");
            var reports = await ctx.CommitteeReports.Include(r => r.Attachments).ToListAsync();
            progress?.Report($"Found {reports.Count} committee report(s).");
            ExportCommitteeReportsToExcel(reports, Path.Combine(dataDir, "committee_reports.xlsx"));

            progress?.Report("Exporting resolutions...");
            var resolutions = await ctx.Resolutions.ToListAsync();
            progress?.Report($"Found {resolutions.Count} resolution(s).");
            ExportResolutionsToExcel(resolutions, Path.Combine(dataDir, "resolutions.xlsx"));

            progress?.Report("Exporting users...");
            var users = await ctx.Users.ToListAsync();
            ExportUsersToExcel(users, Path.Combine(dataDir, "users.xlsx"));

            progress?.Report("Exporting audit log...");
            var auditLogs = await ctx.AuditLogs.ToListAsync();
            ExportAuditLogsToExcel(auditLogs, Path.Combine(dataDir, "audit_logs.xlsx"));

            // Download ordinance PDFs from Supabase Storage
            progress?.Report("Downloading ordinance PDFs...");
            var ordinancePdfDir = Path.Combine(filesDir, "OrdinancePdfs");
            Directory.CreateDirectory(ordinancePdfDir);

            int pdfCount = 0;
            foreach (var o in ordinances.Where(o => !string.IsNullOrEmpty(o.DocumentPath)))
            {
                try
                {
                    var fileName = Path.GetFileName(new Uri(o.DocumentPath!).LocalPath);
                    var dest = Path.Combine(ordinancePdfDir, fileName);
                    await StorageService.DownloadFileAsync(o.DocumentPath!, dest);
                    pdfCount++;
                }
                catch
                {
                    // Skip files that fail to download (e.g. deleted from storage), don't fail the whole backup
                }
            }
            progress?.Report($"Downloaded {pdfCount} ordinance PDF(s).");

            // Download committee report attachments from Supabase Storage
            progress?.Report("Downloading committee report files...");
            var committeeFilesDir = Path.Combine(filesDir, "CommitteeReportFiles");
            Directory.CreateDirectory(committeeFilesDir);

            int attachmentCount = 0;
            foreach (var r in reports)
            {
                foreach (var a in r.Attachments.Where(a => !string.IsNullOrEmpty(a.FilePath)))
                {
                    try
                    {
                        var fileName = Path.GetFileName(new Uri(a.FilePath!).LocalPath);
                        var dest = Path.Combine(committeeFilesDir, fileName);
                        await StorageService.DownloadFileAsync(a.FilePath!, dest);
                        attachmentCount++;
                    }
                    catch
                    {
                        // Skip files that fail to download
                    }
                }
            }
            progress?.Report($"Downloaded {attachmentCount} committee report file(s).");

            // Download resolution documents from Supabase Storage
            progress?.Report("Downloading resolution files...");
            var resolutionFilesDir = Path.Combine(filesDir, "ResolutionFiles");
            Directory.CreateDirectory(resolutionFilesDir);

            int resolutionFileCount = 0;
            foreach (var r in resolutions.Where(r => !string.IsNullOrEmpty(r.DocumentPath)))
            {
                try
                {
                    var fileName = Path.GetFileName(new Uri(r.DocumentPath!).LocalPath);
                    var dest = Path.Combine(resolutionFilesDir, fileName);
                    await StorageService.DownloadFileAsync(r.DocumentPath!, dest);
                    resolutionFileCount++;
                }
                catch
                {
                    // Skip files that fail to download
                }
            }
            progress?.Report($"Downloaded {resolutionFileCount} resolution file(s).");

            // Manifest (small, keep as a simple text summary instead of JSON)
            var manifestPath = Path.Combine(tempDir, "manifest.txt");
            await File.WriteAllTextAsync(manifestPath,
                $"DLIS Backup\n" +
                $"Created: {DateTime.UtcNow:u}\n" +
                $"App Version: {App.CurrentVersion}\n" +
                $"Ordinances: {ordinances.Count}\n" +
                $"Committee Reports: {reports.Count}\n" +
                $"Resolutions: {resolutions.Count}\n" +
                $"Users: {users.Count}\n" +
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

    private static void ExportOrdinancesToExcel(List<Ordinance> ordinances, string path)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Ordinances");

        string[] headers = ["Ordinance Number", "Series", "Title", "Subject", "Type", "Status",
                             "Sponsor", "Committee", "Date Passed", "Date Approved", "Date Published",
                             "Document Path", "Reference Number", "Location", "Version Count"];
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }

        int row = 2;
        foreach (var o in ordinances)
        {
            ws.Cell(row, 1).Value  = o.OrdinanceNumber;
            ws.Cell(row, 2).Value  = o.SeriesNumber;
            ws.Cell(row, 3).Value  = o.Title;
            ws.Cell(row, 4).Value  = o.Subject;
            ws.Cell(row, 5).Value  = o.Type.ToString();
            ws.Cell(row, 6).Value  = o.Status.ToString();
            ws.Cell(row, 7).Value  = o.Sponsor;
            ws.Cell(row, 8).Value  = o.Committee;
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

    private static void ExportCommitteeReportsToExcel(List<CommitteeReport> reports, string path)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("CommitteeReports");

        string[] headers = ["Report Number", "Date", "Submitted By", "Sponsored By", "Subject", "Attachment Count"];
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }

        int row = 2;
        foreach (var r in reports)
        {
            ws.Cell(row, 1).Value = r.ReportNumber;
            ws.Cell(row, 2).Value = r.Date?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(row, 3).Value = r.SubmittedBy;
            ws.Cell(row, 4).Value = r.SponsoredBy;
            ws.Cell(row, 5).Value = r.Subject;
            ws.Cell(row, 6).Value = r.Attachments.Count;
            row++;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(path);
    }

    private static void ExportResolutionsToExcel(List<Resolution> resolutions, string path)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Resolutions");

        string[] headers = ["Resolution Number", "SB Term", "Session Info", "Committee",
                             "Title", "Sponsor", "Date Approved", "Document Path"];
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }

        int row = 2;
        foreach (var r in resolutions)
        {
            ws.Cell(row, 1).Value = r.ResolutionNumber;
            ws.Cell(row, 2).Value = r.SbTerm;
            ws.Cell(row, 3).Value = r.SessionInfo;
            ws.Cell(row, 4).Value = r.Committee;
            ws.Cell(row, 5).Value = r.Title;
            ws.Cell(row, 6).Value = r.Sponsor;
            ws.Cell(row, 7).Value = r.DateApproved?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(row, 8).Value = r.DocumentPath ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(path);
    }

    private static void ExportUsersToExcel(List<User> users, string path)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Users");

        string[] headers = ["Username", "Role"];
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }

        int row = 2;
        foreach (var u in users)
        {
            ws.Cell(row, 1).Value = u.Username;
            ws.Cell(row, 2).Value = u.Role.ToString();
            row++;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(path);
    }

    private static void ExportAuditLogsToExcel(List<AuditLog> logs, string path)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("AuditLogs");

        string[] headers = ["Timestamp", "User", "Action", "Details"];
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }

        int row = 2;
        foreach (var log in logs)
        {
            ws.Cell(row, 1).Value = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            ws.Cell(row, 2).Value = log.Username;
            ws.Cell(row, 3).Value = log.Action;
            ws.Cell(row, 4).Value = log.Details;
            row++;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(path);
    }
}