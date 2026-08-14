namespace LLOIS.Services;

using LLOIS.Models;
using LLOIS.Repositories;

public class CommitteeReportService(ICommitteeReportRepository repo) : ICommitteeReportService
{
    public IEnumerable<CommitteeReport> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return repo.GetAll();

        var keywords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return repo.GetAll().Where(r =>
            keywords.All(k =>
                (r.ReportNumber?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Subject?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.SubmittedBy?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.SponsoredBy?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false)
            ));
    }

    public CommitteeReport? GetDetails(int id) => repo.GetById(id);

    public void Add(CommitteeReport report) => repo.Add(report);

    public void Update(CommitteeReport report) => repo.Update(report);

    public void Delete(int id)
    {
        var report = repo.GetById(id)
            ?? throw new InvalidOperationException("Committee report not found.");
        repo.Delete(report);
    }
}