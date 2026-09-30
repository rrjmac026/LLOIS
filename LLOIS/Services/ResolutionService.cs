namespace LLOIS.Services;

using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

public class ResolutionService(ApiClient api) : IResolutionService
{
    public async Task<IEnumerable<ApiResolution>> SearchAsync(string? query = null)
    {
        var suffix = string.IsNullOrWhiteSpace(query) ? "" : $"?search={Uri.EscapeDataString(query)}";
        var result = await api.GetAsync<PagedResult<ApiResolution>>($"api/resolutions{suffix}");
        return result?.Data ?? Enumerable.Empty<ApiResolution>();
    }

    public async Task<ApiResolution?> GetDetailsAsync(int id)
    {
        var result = await api.GetAsync<ResolutionShowResponse>($"api/resolutions/{id}");
        return result?.Resolution;
    }

    public async Task<ApiResolution> AddAsync(ResolutionInput input) =>
        await SendAsync<ApiResolution>("api/resolutions", input)
            ?? throw new InvalidOperationException("Unexpected empty response creating resolution.");

    public Task UpdateAsync(int id, ResolutionInput input) =>
        SendAsync<object>($"api/resolutions/{id}?_method=PUT", input);

    public Task DeleteAsync(int id) =>
        api.DeleteAsync($"api/resolutions/{id}");

    private async Task<T?> SendAsync<T>(string endpoint, ResolutionInput input)
    {
        using var content = new MultipartFormDataContent();

        content.Add(new StringContent(input.ResolutionNumber), "resolution_number");
        content.Add(new StringContent(input.Title), "title");
        // Always send optional fields (empty string when null). Laravel converts "" to null,
        // which is what lets a user clear a field on edit. Skipping them would keep the old value.
        content.Add(new StringContent(input.SbTerm ?? ""), "sb_term");
        content.Add(new StringContent(input.SessionInfo ?? ""), "session_info");
        content.Add(new StringContent(input.Committee ?? ""), "committee");
        content.Add(new StringContent(input.Sponsor ?? ""), "sponsor");
        content.Add(new StringContent(input.DateApproved?.ToString("yyyy-MM-dd") ?? ""), "date_approved");

        if (input.LocalDocumentPath is not null)
        {
            var bytes = await File.ReadAllBytesAsync(input.LocalDocumentPath);
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(input.LocalDocumentPath));
            content.Add(fileContent, "document", Path.GetFileName(input.LocalDocumentPath));
        }

        return await api.PostMultipartAsync<T>(endpoint, content);
    }

    private static string GetContentType(string filePath) => Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".pdf"  => "application/pdf",
        ".doc"  => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        _       => "application/octet-stream"
    };
}