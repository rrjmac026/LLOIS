using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Models;

// CommitteeReport.cs
public class CommitteeReport
{
    public int Id { get; set; }
    public string ReportNumber { get; set; } = string.Empty;
    public DateOnly? Date { get; set; }
    public string SubmittedBy { get; set; } = string.Empty;
    public string SponsoredBy { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;

    public string? AddedBy { get; set; }
    public DateTime? AddedAt { get; set; }

    public List<CommitteeReportAttachment> Attachments { get; set; } = [];
}

public class CommitteeReportAttachment
{
    public int Id { get; set; }
    public int CommitteeReportId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;   // was DateTime.Now
}