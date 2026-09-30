namespace LLOIS.Services;

public interface IMinutesService
{
    Task<IEnumerable<ApiMinutes>> GetAllAsync();
    Task<ApiMinutes?> GetDetailsAsync(int id);
    Task<ApiMinutes> AddAsync(MinutesInput input);
    Task UpdateAsync(int id, MinutesInput input);
    Task DeleteAsync(int id);
}

public class MinutesInput
{
    // Laravel only accepts "Regular Session" or "Special Session".
    public string SessionType { get; set; } = string.Empty;
    public DateOnly? Date { get; set; }

    // Local file path of a newly picked PDF/DOC/DOCX to upload with this request.
    // Not sent if null (keeps the existing document on update).
    public string? LocalDocumentPath { get; set; }
}

public class ApiMinutes
{
    public int Id { get; set; }
    public string SessionType { get; set; } = string.Empty;
    public DateOnly? Date { get; set; }
    public string? DocumentPath { get; set; }
}

// Laravel's show() returns { "minutes": {...}, "document_url": "..." },
// not the bare record, so GetDetailsAsync has to unwrap it.
public class MinutesShowResponse
{
    public ApiMinutes? Minutes { get; set; }
    public string? DocumentUrl { get; set; }
}