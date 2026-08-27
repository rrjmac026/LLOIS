namespace LLOIS.Services;

using LLOIS.Models;
using LLOIS.Repositories;

public class FeedbackService(IFeedbackRepository repo) : IFeedbackService
{
    public IEnumerable<Feedback> GetAll() => repo.GetAll();

    public void Submit(Feedback feedback) => repo.Add(feedback);

    public void MarkResolved(int id) => repo.MarkResolved(id);
}