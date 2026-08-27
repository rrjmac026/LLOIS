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

public enum OrdinanceState
{
    Draft,
    Passed,
    Enacted
}

public enum TypeOfLaw
{
    Resolution,
    Ordinance,
    Minutes
}

public enum FinalAction
{
    Approving,
    Authorizing,
    Creating,
    Declaring,
    Conducting,
    Extending
}

public enum UserRole
{
    Viewer = 0,
    Encoder = 1,
    Admin = 2,
    SuperAdmin = 3
}