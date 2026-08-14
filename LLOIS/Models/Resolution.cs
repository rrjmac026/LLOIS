namespace LLOIS.Models;

public class Resolution
{
    public int Id { get; set; }

    public string ResolutionNumber { get; set; } = string.Empty;
    public string SbTerm { get; set; } = string.Empty;
    public string SessionInfo { get; set; } = string.Empty;
    public string Committee { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Sponsor { get; set; } = string.Empty;

    public DateOnly? DateApproved { get; set; }

    public string AffirmativeVotes { get; set; } = string.Empty;
    public string NegativeVotes    { get; set; } = "None";
    public string AbstainedVotes   { get; set; } = "None";
    public string AbsentVotes      { get; set; } = "None";

    public string CertifiedAdoptedBy { get; set; } = string.Empty;
    public DateOnly? CertifiedDate { get; set; }

    public string VerifiedBy { get; set; } = string.Empty;
    public DateOnly? VerifiedDate { get; set; }

    public string AttestedBy { get; set; } = string.Empty;
    public DateOnly? AttestedDate { get; set; }

    public string? DocumentPath { get; set; }
    public string? AddedBy { get; set; }
    public DateTime? AddedAt { get; set; }

    // Single combined list, mapped to the ResolutionClauses table
    public List<ResolutionClause> Clauses { get; set; } = [];

    // Convenience filtered views — not separately mapped to the DB
    public IEnumerable<ResolutionClause> WhereasClauses =>
        Clauses.Where(c => c.ClauseType == "Whereas").OrderBy(c => c.Order);

    public IEnumerable<ResolutionClause> ResolvedClauses =>
        Clauses.Where(c => c.ClauseType == "Resolved").OrderBy(c => c.Order);
}

public class ResolutionClause
{
    public int Id { get; set; }
    public int ResolutionId { get; set; }
    public string ClauseType { get; set; } = string.Empty; // "Whereas" or "Resolved"
    public int Order { get; set; }
    public string Text { get; set; } = string.Empty;
}