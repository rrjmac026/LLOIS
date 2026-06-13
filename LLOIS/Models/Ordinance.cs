using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Models;

public class Ordinance
{
    public string Id { get; set; } = string.Empty;         // e.g. "ORD-2020-001"
    public string SeriesNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public OrdinanceStatus Status { get; set; }
    public List<OrdinanceVersion> Versions { get; set; } = [];

    public OrdinanceVersion? LatestVersion =>
        Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

    public bool HasAmendments => Versions.Count > 1;
}
