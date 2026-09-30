namespace LLOIS.Services;

using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

public class MinutesService(ApiClient api) : IMinutesService
{
    public async Task<IEnumerable<ApiMinutes>> GetAllAsync()
    {
        var result = await api.GetAsync<PagedResult<ApiMinutes>>("api/minutes");
        return result?.Data ?? Enumerable.Empty<ApiMinutes>();
    }

    public async Task<ApiMinutes?> GetDetailsAsync(int id)
    {
        var result = await api.GetAsync<MinutesShowResponse>($"api/minutes/{id}");
        return result?.Minutes;
    }

    public async Task<ApiMinutes> AddAsync(MinutesInput input) =>
        await SendAsync<ApiMinutes>("api/minutes", input)
            ?? throw new InvalidOperationException("Unexpected empty response creating minutes.");

    public Task UpdateAsync(int id, MinutesInput input) =>
        SendAsync<object>($"api/minutes/{id}?_method=PUT", input);

    public Task DeleteAsync(int id) =>
        api.DeleteAsync($"api/minutes/{id}");

    private async Task<T?> SendAsync<T>(string endpoint, MinutesInput input)
    {
        using var content = new MultipartFormDataContent();

        content.Add(new StringContent(input.SessionType), "session_type");
        // Always sent ("" when null) so a date can be cleared on edit; Laravel turns "" into null.
        content.Add(new StringContent(input.Date?.ToString("yyyy-MM-dd") ?? ""), "date");

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