using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Models;

public class Minutes
{
    public int Id { get; set; }
    public string SessionType { get; set; } = string.Empty; // "Regular Session" or "Special Session"
    public DateOnly? Date { get; set; }
    public string? DocumentPath { get; set; }
}