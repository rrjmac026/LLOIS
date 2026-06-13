namespace LLOIS.Services;

using LLOIS.Models;

public interface IOrdinanceService
{
    IEnumerable<Ordinance> Search(string query);
    Ordinance? GetDetails(string id);
    void Add(Ordinance ordinance);
    void Update(Ordinance ordinance);
    void Delete(string ordinanceNumber);
    void AddAmendment(string ordinanceId, OrdinanceVersion newVersion);
    void UpdateStatus(string ordinanceId, OrdinanceStatus status);
    IEnumerable<Ordinance> GetByYear(int year);
    IEnumerable<Ordinance> GetByStatus(OrdinanceStatus status);
    IEnumerable<int> GetAvailableYears();
}
