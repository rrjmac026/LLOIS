namespace LLOIS.Repositories;

using LLOIS.Models;

public interface ICommitteeReportRepository
{
    IEnumerable<CommitteeReport> GetAll();
    CommitteeReport? GetById(int id);
    IEnumerable<CommitteeReport> Search(string query);
    void Add(CommitteeReport report);
    void Update(CommitteeReport report);
    void Delete(CommitteeReport report);
}