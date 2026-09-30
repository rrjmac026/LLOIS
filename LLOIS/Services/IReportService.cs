namespace LLOIS.Services;

public interface IReportService
{
    Task<List<int>> GetOrdinanceYearsAsync();

    // Full (unpaginated) list. status = raw Laravel value, e.g. "in_effect".
    Task<List<ApiOrdinance>> GetOrdinancesAsync(string? status = null, int? year = null, bool amendedOnly = false);

    // Exports/prints happen locally, so the server never sees them — this reports them
    // so the audit log stays complete. Laravel only accepts a fixed list of action names.
    Task LogReportActionAsync(string action, string details);
}