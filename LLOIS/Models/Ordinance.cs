using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Models;

public class Ordinance
{
    public int Id { get; set; }
    public string OrdinanceNumber { get; set; } = string.Empty;
    public string SeriesNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public OrdinanceType Type { get; set; }
    public OrdinanceStatus Status { get; set; }

    // Metadata
    public string Sponsor { get; set; } = string.Empty;
    public string Committee { get; set; } = string.Empty;
    public DateOnly? DatePassed { get; set; }
    public DateOnly? DateApprovedByMayor { get; set; }
    public DateOnly? DatePublished { get; set; }

    // PDF attachment path
    public string? DocumentPath { get; set; }

    // Relationships
    public string? AmendsOrdinanceNumber { get; set; }
    public string? SupersedesOrdinanceNumber { get; set; }
    public string? RepealedByOrdinanceNumber { get; set; }
    public string? RepealReason { get; set; }

    public List<OrdinanceVersion> Versions { get; set; } = [];

    public OrdinanceVersion? LatestVersion =>
        Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

    public bool HasAmendments => Versions.Count > 1;
}
