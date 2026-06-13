using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Services;

using LLOIS.Models;
using LLOIS.Repositories;

public class OrdinanceService(IOrdinanceRepository repo) : IOrdinanceService
{
    public IEnumerable<Ordinance> Search(string query) =>
        string.IsNullOrWhiteSpace(query) ? repo.GetAll() : repo.Search(query);

    public Ordinance? GetDetails(string id) => repo.GetById(id);

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
}
