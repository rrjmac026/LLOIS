namespace LLOIS.Services;

public class ReportService(ApiClient api) : IReportService
{
    public async Task<List<int>> GetOrdinanceYearsAsync() =>
        await api.GetAsync<List<int>>("api/reports/ordinance-years") ?? [];

    public async Task<List<ApiOrdinance>> GetOrdinancesAsync(
        string? status = null, int? year = null, bool amendedOnly = false)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(status)) qs.Add($"status={Uri.EscapeDataString(status)}");
        if (year.HasValue) qs.Add($"year={year.Value}");
        if (amendedOnly) qs.Add("amended=1");
        var suffix = qs.Count > 0 ? "?" + string.Join("&", qs) : "";

        return await api.GetAsync<List<ApiOrdinance>>($"api/reports/ordinances{suffix}") ?? [];
    }

    public Task LogReportActionAsync(string action, string details) =>
        api.PostAsync<object>("api/audit-logs", new { action, details });
}