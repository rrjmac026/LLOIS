namespace LLOIS.Services;

public interface IResolutionService
{
    Task<IEnumerable<ApiResolution>> SearchAsync(string? query = null);
    Task<ApiResolution?> GetDetailsAsync(int id);
    Task<ApiResolution> AddAsync(ResolutionInput input);
    Task UpdateAsync(int id, ResolutionInput input);
    Task DeleteAsync(int id);
}

public class ResolutionInput
{
    public string ResolutionNumber { get; set; } = string.Empty;
    public string? SbTerm { get; set; }
    public string? SessionInfo { get; set; }
    public string? Committee { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Sponsor { get; set; }
    public DateOnly? DateApproved { get; set; }

    // Local file path of a newly picked PDF/DOC/DOCX to upload with this request.
    // Not sent if null (keeps the existing document on update).
    public string? LocalDocumentPath { get; set; }
}

public class ApiResolution
{
    public int Id { get; set; }
    public string ResolutionNumber { get; set; } = string.Empty;
    public string? SbTerm { get; set; }
    public string? SessionInfo { get; set; }
    public string? Committee { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Sponsor { get; set; }
    public DateOnly? DateApproved { get; set; }
    public string? DocumentPath { get; set; }
    public string? AddedBy { get; set; }
    public DateTime? AddedAt { get; set; }
}

// Laravel's show() returns { "resolution": {...}, "document_url": "..." },
// not the bare resolution, so GetDetailsAsync has to unwrap it.
public class ResolutionShowResponse
{
    public ApiResolution? Resolution { get; set; }
    public string? DocumentUrl { get; set; }
}