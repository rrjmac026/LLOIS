namespace LLOIS.Services;

using LLOIS.Models;
using LLOIS.Repositories;

public class OrdinanceService(IOrdinanceRepository repo) : IOrdinanceService
{
    public IEnumerable<Ordinance> Search(string query) =>
        string.IsNullOrWhiteSpace(query) ? repo.GetAll() : repo.Search(query);

    public Ordinance? GetDetails(string id) => repo.GetById(id);

    public void Add(Ordinance ordinance) => repo.Add(ordinance);

    public void Update(Ordinance ordinance) => repo.Update(ordinance);

    public void Delete(string ordinanceNumber)
    {
        var ordinance = repo.GetById(ordinanceNumber)
            ?? throw new InvalidOperationException("Ordinance not found.");
        repo.Delete(ordinance);
    }

    public void AddAmendment(string ordinanceId, OrdinanceVersion newVersion)
    {
        var ordinance = repo.GetById(ordinanceId)
            ?? throw new InvalidOperationException("Ordinance not found.");

        newVersion.VersionNumber = ordinance.Versions.Count + 1;
        ordinance.Versions.Add(newVersion);
        ordinance.Status = OrdinanceStatus.Amended;

        repo.Update(ordinance);
    }

    public void UpdateStatus(string ordinanceId, OrdinanceStatus status)
    {
        var ordinance = repo.GetById(ordinanceId)
            ?? throw new InvalidOperationException("Ordinance not found.");

        ordinance.Status = status;
        repo.Update(ordinance);
    }

    public IEnumerable<Ordinance> GetByYear(int year) =>
        repo.GetAll().Where(o => o.DatePassed?.Year == year);

    public IEnumerable<Ordinance> GetByStatus(OrdinanceStatus status) =>
        repo.GetAll().Where(o => o.Status == status);

    public IEnumerable<int> GetAvailableYears() =>
        repo.GetAll()
            .Where(o => o.DatePassed.HasValue)
            .Select(o => o.DatePassed!.Value.Year)
            .Distinct()
            .OrderByDescending(y => y);
}
