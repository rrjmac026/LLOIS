namespace LLOIS.Services;

public interface IFeedbackService
{
    Task<IEnumerable<ApiFeedback>> GetAllAsync();
    Task SubmitAsync(ApiFeedbackType type, string message);
    Task MarkResolvedAsync(int id);
}