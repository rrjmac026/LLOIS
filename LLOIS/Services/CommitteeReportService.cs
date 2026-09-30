namespace LLOIS.Services;

using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

public class CommitteeReportService(ApiClient api) : ICommitteeReportService
{
    public async Task<IEnumerable<ApiCommitteeReport>> SearchAsync(string? query = null)
    {
        var suffix = string.IsNullOrWhiteSpace(query) ? "" : $"?search={Uri.EscapeDataString(query)}";
        var result = await api.GetAsync<PagedResult<ApiCommitteeReport>>($"api/committee-reports{suffix}");
        return result?.Data ?? Enumerable.Empty<ApiCommitteeReport>();
    }

    public Task<ApiCommitteeReportDetails?> GetDetailsAsync(int id) =>
        api.GetAsync<ApiCommitteeReportDetails>($"api/committee-reports/{id}");

    public Task<ApiCommitteeReport> AddAsync(CommitteeReportInput input) =>
        SendAsync<ApiCommitteeReport>("api/committee-reports", input)!;

    public Task UpdateAsync(int id, CommitteeReportInput input) =>
        SendAsync<object>($"api/committee-reports/{id}?_method=PUT", input);

    public Task DeleteAsync(int id) =>
        api.DeleteAsync($"api/committee-reports/{id}");

    public Task DeleteAttachmentAsync(int reportId, int attachmentId) =>
        api.DeleteAsync($"api/committee-reports/{reportId}/attachments/{attachmentId}");

    private async Task<T?> SendAsync<T>(string endpoint, CommitteeReportInput input)
    {
        using var content = new MultipartFormDataContent();

        content.Add(new StringContent(input.ReportNumber), "report_number");
        if (input.Date.HasValue)
            content.Add(new StringContent(input.Date.Value.ToString("yyyy-MM-dd")), "date");
        if (input.SubmittedBy is not null)
            content.Add(new StringContent(input.SubmittedBy), "submitted_by");
        if (input.SponsoredBy is not null)
            content.Add(new StringContent(input.SponsoredBy), "sponsored_by");
        if (input.Subject is not null)
            content.Add(new StringContent(input.Subject), "subject");

        foreach (var path in input.NewAttachmentFilePaths)
        {
            var bytes = await File.ReadAllBytesAsync(path);
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(path));
            content.Add(fileContent, "attachments[]", Path.GetFileName(path));
        }

        return await api.PostMultipartAsync<T>(endpoint, content);
    }

    private static string GetContentType(string filePath) => Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".pdf"  => "application/pdf",
        ".doc"  => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls"  => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".png"  => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".txt"  => "text/plain",
        _       => "application/octet-stream"
    };
}