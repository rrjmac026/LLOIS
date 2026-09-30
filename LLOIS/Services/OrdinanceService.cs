namespace LLOIS.Services;

using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

public class OrdinanceService(ApiClient api) : IOrdinanceService
{
    public async Task<IEnumerable<ApiOrdinance>> SearchAsync(string? query = null, string? status = null)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(query)) qs.Add($"search={Uri.EscapeDataString(query)}");
        if (!string.IsNullOrWhiteSpace(status)) qs.Add($"status={Uri.EscapeDataString(status)}");
        var suffix = qs.Count > 0 ? "?" + string.Join("&", qs) : "";

        var result = await api.GetAsync<PagedResult<ApiOrdinance>>($"api/ordinances{suffix}");
        return result?.Data ?? Enumerable.Empty<ApiOrdinance>();
    }

    public Task<ApiOrdinance?> GetDetailsAsync(int id) =>
        api.GetAsync<ApiOrdinance>($"api/ordinances/{id}");

    public async Task<ApiOrdinance> AddAsync(OrdinanceInput input)
    {
        var ordinance = await SendAsync<ApiOrdinance>("api/ordinances", input)
            ?? throw new InvalidOperationException("Unexpected empty response creating ordinance.");

        if (input.InitialVersion is not null)
        {
            await api.PostAsync<object>($"api/ordinances/{ordinance.Id}/versions", input.InitialVersion);
            var withVersion = await GetDetailsAsync(ordinance.Id);
            if (withVersion is not null) return withVersion;
        }

        return ordinance;
    }

    public Task UpdateAsync(int id, OrdinanceInput input) =>
        SendAsync<object>($"api/ordinances/{id}?_method=PUT", input);

    public Task DeleteAsync(int id) =>
        api.DeleteAsync($"api/ordinances/{id}");

    private async Task<T?> SendAsync<T>(string endpoint, OrdinanceInput input)
    {
        using var content = new MultipartFormDataContent();

        content.Add(new StringContent(input.OrdinanceNumber), "ordinance_number");
        if (input.SeriesNumber is not null) content.Add(new StringContent(input.SeriesNumber), "series_number");
        content.Add(new StringContent(input.Title), "title");
        if (input.Subject is not null) content.Add(new StringContent(input.Subject), "subject");
        content.Add(new StringContent(input.Type), "type");
        content.Add(new StringContent(input.Status), "status");
        if (input.Sponsor is not null) content.Add(new StringContent(input.Sponsor), "sponsor");
        if (input.Committee is not null) content.Add(new StringContent(input.Committee), "committee");
        if (input.DatePassed.HasValue) content.Add(new StringContent(input.DatePassed.Value.ToString("yyyy-MM-dd")), "date_passed");
        if (input.DateApproved.HasValue) content.Add(new StringContent(input.DateApproved.Value.ToString("yyyy-MM-dd")), "date_approved");
        if (input.DatePublished.HasValue) content.Add(new StringContent(input.DatePublished.Value.ToString("yyyy-MM-dd")), "date_published");
        if (input.ReferenceNumber is not null) content.Add(new StringContent(input.ReferenceNumber), "reference_number");
        if (input.NrsNsb is not null) content.Add(new StringContent(input.NrsNsb), "nrs_nsb");
        if (input.Nomenclature is not null) content.Add(new StringContent(input.Nomenclature), "nomenclature");
        if (input.FinalAction is not null) content.Add(new StringContent(input.FinalAction), "final_action");
        if (input.Location is not null) content.Add(new StringContent(input.Location), "location");
        if (input.State is not null) content.Add(new StringContent(input.State), "state");

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