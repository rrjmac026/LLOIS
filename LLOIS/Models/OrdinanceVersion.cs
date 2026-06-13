using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Models;

public class OrdinanceVersion
{
    public int VersionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateOnly DateEnacted { get; set; }
    public string EnactedBy { get; set; } = string.Empty; // e.g. "Ordinance No. 2024-05"
    public string? AmendmentNotes { get; set; }
}
