using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Models
{
    public enum OrdinanceStatus
    {
        InEffect,
        Amended,
        Superseded,
        Repealed
    }

    public enum UserRole
    {
        Admin,
        Encoder,
        Viewer
    }
}
