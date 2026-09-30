namespace LLOIS.Services;

public class FeedbackService(ApiClient api) : IFeedbackService
{
    public async Task<IEnumerable<ApiFeedback>> GetAllAsync()
    {
        var result = await api.GetAsync<PagedResult<ApiFeedback>>("api/feedback");
        return result?.Data ?? Enumerable.Empty<ApiFeedback>();
    }

    // Laravel's backed enums use lowercase string values.
    public Task SubmitAsync(ApiFeedbackType type, string message) =>
        api.PostAsync<object>("api/feedback", new { type = ToApiValue(type), message });

    public Task MarkResolvedAsync(int id) =>
        api.PostAsync<object>($"api/feedback/{id}?_method=PUT",
            new { status = "resolved" });

    private static string ToApiValue(ApiFeedbackType type) => type switch
    {
        ApiFeedbackType.Bug => "bug",
        ApiFeedbackType.Concern => "concern",
        ApiFeedbackType.Suggestion => "suggestion",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}