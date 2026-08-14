namespace LLOIS.Repositories;

using LLOIS.Data;
using LLOIS.Models;
using Microsoft.EntityFrameworkCore;

public class CommitteeReportRepository(SimpleDbContextFactory dbFactory) : ICommitteeReportRepository
{
    public IEnumerable<CommitteeReport> GetAll()
    {
        using var ctx = dbFactory.CreateDbContext();
        return ctx.CommitteeReports
            .Include(r => r.Attachments)
            .OrderByDescending(r => r.Date)
            .ToList();
    }

    public CommitteeReport? GetById(int id)
    {
        using var ctx = dbFactory.CreateDbContext();
        return ctx.CommitteeReports
            .Include(r => r.Attachments)
            .FirstOrDefault(r => r.Id == id);
    }

    public IEnumerable<CommitteeReport> Search(string query)
    {
        using var ctx = dbFactory.CreateDbContext();
        return ctx.CommitteeReports
            .Include(r => r.Attachments)
            .Where(r =>
                r.ReportNumber.Contains(query) ||
                r.Subject.Contains(query) ||
                r.SubmittedBy.Contains(query) ||
                r.SponsoredBy.Contains(query))
            .ToList();
    }

    public void Add(CommitteeReport report)
    {
        using var ctx = dbFactory.CreateDbContext();
        ctx.CommitteeReports.Add(report);
        ctx.SaveChanges();
    }

    public void Update(CommitteeReport report)
    {
        using var ctx = dbFactory.CreateDbContext();
        ctx.CommitteeReports.Update(report);
        ctx.SaveChanges();
    }

    public void Delete(CommitteeReport report)
    {
        using var ctx = dbFactory.CreateDbContext();
        ctx.CommitteeReports.Remove(report);
        ctx.SaveChanges();
    }
}