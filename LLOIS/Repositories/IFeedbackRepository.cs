namespace LLOIS.Repositories;

using LLOIS.Models;

public interface IFeedbackRepository
{
    IEnumerable<Feedback> GetAll();
    Feedback? GetByPrimaryId(int id);
    void Add(Feedback feedback);
    void Update(Feedback feedback);
    void MarkResolved(int id);
}