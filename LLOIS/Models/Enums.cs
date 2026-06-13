using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Models;

public enum OrdinanceStatus
{
    InEffect,
    Amended,
    Superseded,
    Repealed,
    UnderReview
}

public enum OrdinanceType
{
    Regulatory,
    Revenue,
    Administrative,
    Penal,
    Appropriation,
    Other
}

public enum UserRole
{
    Admin,
    Encoder,
    Viewer
}