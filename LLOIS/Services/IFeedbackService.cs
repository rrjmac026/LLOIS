namespace LLOIS.Services;

using LLOIS.Models;

public interface IFeedbackService
{
    IEnumerable<Feedback> GetAll();
    void Submit(Feedback feedback);
    void MarkResolved(int id);
}