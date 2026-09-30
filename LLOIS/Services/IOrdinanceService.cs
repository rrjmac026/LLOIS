namespace LLOIS.Services;

public interface IOrdinanceService
{
    Task<IEnumerable<ApiOrdinance>> SearchAsync(string? query = null, string? status = null);
    Task<ApiOrdinance?> GetDetailsAsync(int id);
    Task<ApiOrdinance> AddAsync(OrdinanceInput input);
    Task UpdateAsync(int id, OrdinanceInput input);
    Task DeleteAsync(int id);
}

public class OrdinanceInput
{
    public string OrdinanceNumber { get; set; } = string.Empty;
    public string? SeriesNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Type { get; set; } = "ordinance";
    public string Status { get; set; } = "under_review";
    public string? Sponsor { get; set; }
    public string? Committee { get; set; }
    public DateOnly? DatePassed { get; set; }
    public DateOnly? DateApproved { get; set; }
    public DateOnly? DatePublished { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? NrsNsb { get; set; }
    public string? Nomenclature { get; set; }
    public string? FinalAction { get; set; }
    public string? Location { get; set; }
    public string? State { get; set; }

    // Local file path of a newly picked PDF to upload with this request.
    // Not sent if null (keeps existing document on update).
    public string? LocalDocumentPath { get; set; }

    // Set only on create, to chain a storeVersion call for the initial version.
    public OrdinanceVersionInput? InitialVersion { get; set; }
}

public class OrdinanceVersionInput
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateOnly DateEnacted { get; set; }
    public string EnactedBy { get; set; } = string.Empty;
    public string? AmendmentNotes { get; set; }
}

public class ApiOrdinance
{
    public int Id { get; set; }
    public string OrdinanceNumber { get; set; } = string.Empty;
    public string? SeriesNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Sponsor { get; set; }
    public string? Committee { get; set; }
    public DateOnly? DatePassed { get; set; }
    public DateOnly? DateApproved { get; set; }
    public DateOnly? DatePublished { get; set; }
    public string? DocumentPath { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? NrsNsb { get; set; }
    public string? Nomenclature { get; set; }
    public string? FinalAction { get; set; }
    public string? Location { get; set; }
    public string? State { get; set; }
    public List<ApiOrdinanceVersion> Versions { get; set; } = new();
    public string? AddedBy { get; set; }
    public DateTime? AddedAt { get; set; }

    public ApiOrdinanceVersion? LatestVersion =>
        Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
    public bool HasAmendments => Versions.Count > 1;
}

public class ApiOrdinanceVersion
{
    public int Id { get; set; }
    public int VersionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}