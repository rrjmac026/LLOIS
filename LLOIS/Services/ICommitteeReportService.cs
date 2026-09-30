namespace LLOIS.Services;

public interface ICommitteeReportService
{
    Task<IEnumerable<ApiCommitteeReport>> SearchAsync(string? query = null);
    Task<ApiCommitteeReportDetails?> GetDetailsAsync(int id);
    Task<ApiCommitteeReport> AddAsync(CommitteeReportInput input);
    Task UpdateAsync(int id, CommitteeReportInput input);
    Task DeleteAsync(int id);
    Task DeleteAttachmentAsync(int reportId, int attachmentId);
}

public class CommitteeReportInput
{
    public string ReportNumber { get; set; } = string.Empty;
    public DateOnly? Date { get; set; }
    public string? SubmittedBy { get; set; }
    public string? SponsoredBy { get; set; }
    public string? Subject { get; set; }

    // Local file paths of newly picked attachments to upload with this
    // request. Existing attachments already on the server don't go here —
    // remove them with DeleteAttachmentAsync instead.
    public List<string> NewAttachmentFilePaths { get; set; } = [];
}

public class ApiCommitteeReport
{
    public int Id { get; set; }
    public string ReportNumber { get; set; } = string.Empty;
    public DateOnly? Date { get; set; }
    public string? SubmittedBy { get; set; }
    public string? SponsoredBy { get; set; }
    public string? Subject { get; set; }
    public string? AddedBy { get; set; }
    public DateTime? AddedAt { get; set; }
    public int AttachmentsCount { get; set; } // only populated by SearchAsync's list view
}

public class ApiCommitteeReportDetails
{
    public ApiCommitteeReport Report { get; set; } = new();
    public List<ApiCommitteeReportAttachment> Attachments { get; set; } = new();
}

public class ApiCommitteeReportAttachment
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? ViewUrl { get; set; }
}