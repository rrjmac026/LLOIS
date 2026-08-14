namespace LLOIS.Services;

using LLOIS.Models;

public interface ICommitteeReportService
{
    IEnumerable<CommitteeReport> Search(string query);
    CommitteeReport? GetDetails(int id);
    void Add(CommitteeReport report);
    void Update(CommitteeReport report);
    void Delete(int id);
}