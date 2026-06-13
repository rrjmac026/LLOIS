using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Services;

using LLOIS.Models;

public interface IOrdinanceService
{
    IEnumerable<Ordinance> Search(string query);
    Ordinance? GetDetails(string id);
    void AddAmendment(string ordinanceId, OrdinanceVersion newVersion);
    void UpdateStatus(string ordinanceId, OrdinanceStatus status);
}
