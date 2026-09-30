namespace LLOIS.Services;

public interface IDashboardService
{
    Task<ApiDashboard> GetAsync();
}

public class ApiDashboard
{
    public ApiDashboardOrdinances Ordinances { get; set; } = new();
    public ApiDashboardCount Resolutions { get; set; } = new();
    public ApiDashboardCount CommitteeReports { get; set; } = new();
    public ApiDashboardCount Minutes { get; set; } = new();
    public ApiDashboardUsers? Users { get; set; }               // null unless Admin+
    public List<ApiDashboardRecent> Recent { get; set; } = new();
    public List<ApiDashboardActivity> RecentActivity { get; set; } = new();
    public string ActivityScope { get; set; } = "own";          // "all" or "own"
}

public class ApiDashboardCount
{
    public int Total { get; set; }
    public int ThisYear { get; set; }
}

public class ApiDashboardUsers
{
    public int Total { get; set; }
    public int Active { get; set; }
}

public class ApiDashboardOrdinances : ApiDashboardCount
{
    // Keyed by the raw status value Laravel stores (e.g. "in_effect", "under_review").
    public Dictionary<string, int> ByStatus { get; set; } = new();

    // Matches "in_effect", "InEffect", "in effect"... so a naming difference can't zero out a card.
    public int CountFor(string status)
    {
        static string Norm(string s) => s.Replace("_", "").Replace(" ", "").ToLowerInvariant();
        var wanted = Norm(status);
        return ByStatus.Where(kv => Norm(kv.Key) == wanted).Sum(kv => kv.Value);
    }
}

public class ApiDashboardRecent
{
    public string Type { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public DateOnly Date { get; set; }
}

public class ApiDashboardActivity
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? Source { get; set; }
    public DateTime? CreatedAt { get; set; }
}