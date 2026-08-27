namespace LLOIS.Repositories;

using Microsoft.EntityFrameworkCore;
using LLOIS.Data;
using LLOIS.Models;

public class FeedbackRepository(IDbContextFactory<AppDbContext> dbFactory) : IFeedbackRepository
{
    public IEnumerable<Feedback> GetAll()
    {
        using var db = dbFactory.CreateDbContext();
        return db.Feedback.OrderByDescending(f => f.CreatedAt).ToList();
    }

    public Feedback? GetByPrimaryId(int id)
    {
        using var db = dbFactory.CreateDbContext();
        return db.Feedback.FirstOrDefault(f => f.Id == id);
    }

    public void Add(Feedback feedback)
    {
        using var db = dbFactory.CreateDbContext();
        db.Feedback.Add(feedback);
        db.SaveChanges();
    }

    public void Update(Feedback feedback)
    {
        using var db = dbFactory.CreateDbContext();
        db.Feedback.Update(feedback);
        db.SaveChanges();
    }

    public void MarkResolved(int id)
    {
        using var db = dbFactory.CreateDbContext();
        var feedback = db.Feedback.Find(id)
            ?? throw new InvalidOperationException("Feedback not found.");

        // Ensure CreatedAt has UTC kind before EF re-saves the row —
        // guards against rows written before the model used DateTime.UtcNow.
        if (feedback.CreatedAt.Kind != DateTimeKind.Utc)
            feedback.CreatedAt = DateTime.SpecifyKind(feedback.CreatedAt, DateTimeKind.Utc);

        feedback.Status = FeedbackStatus.Resolved;
        db.SaveChanges();
    }
}