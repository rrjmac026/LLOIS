namespace LLOIS.Services;

// Prefixed "Api" so the form enum doesn't clash with the old LLOIS.Models enum.
public enum ApiFeedbackType   { Bug = 0, Concern = 1, Suggestion = 2 }

public class ApiFeedback
{
    public int Id { get; set; }
    public string SubmittedBy { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    // Laravel sends UTC ("...Z"); convert once here so every binding shows local time.
    private DateTime _createdAt;
    public DateTime CreatedAt
    {
        get => _createdAt;
        set => _createdAt = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
    }
}