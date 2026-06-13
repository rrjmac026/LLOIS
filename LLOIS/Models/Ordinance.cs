using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Models;

public class Ordinance
{
    public int Id { get; set; }
    public string OrdinanceNumber { get; set; } = string.Empty;
    public string SeriesNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public OrdinanceStatus Status { get; set; }
    public List<OrdinanceVersion> Versions { get; set; } = [];

    public OrdinanceVersion? LatestVersion =>
        Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

    public bool HasAmendments => Versions.Count > 1;
}
