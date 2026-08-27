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
    public TypeOfLaw Type { get; set; }
    public OrdinanceStatus Status { get; set; }

    // Metadata
    public string Sponsor { get; set; } = string.Empty;
    public string Committee { get; set; } = string.Empty;
    public DateOnly? DatePassed { get; set; }
    public DateOnly? DateApproved { get; set; }
    public DateOnly? DatePublished { get; set; }

    // PDF attachment path
    public string? DocumentPath { get; set; }

    // New fields
    public string? ReferenceNumber { get; set; }
    public string? NRS_NSB { get; set; }
    public string? Nomenclature { get; set; }
    public FinalAction? FinalAction { get; set; }
    public string? Location { get; set; }
    public OrdinanceState? State { get; set; }

    public List<OrdinanceVersion> Versions { get; set; } = [];

    public OrdinanceVersion? LatestVersion =>
        Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

    public bool HasAmendments => Versions.Count > 1;

    public string? AddedBy { get; set; }
    public DateTime? AddedAt { get; set; }
}