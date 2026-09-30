namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LLOIS.Services;

public partial class DashboardView : UserControl
{
    private readonly IDashboardService _dashboard;
    private readonly ApiUser           _currentUser;

    public event Action? NavigateToOrdinances;
    public event Action? NavigateToResolutions;
    public event Action? NavigateToCommitteeReports;

    public DashboardView(IDashboardService dashboard, ApiUser user)
    {
        InitializeComponent();
        _dashboard   = dashboard;
        _currentUser = user;
    }

    public void Refresh() => _ = LoadAsync();

    private void OrdinancesCard_Click(object sender, MouseButtonEventArgs e)
        => NavigateToOrdinances?.Invoke();

    private void ResolutionsCard_Click(object sender, MouseButtonEventArgs e)
        => NavigateToResolutions?.Invoke();

    private void CommitteeReportsCard_Click(object sender, MouseButtonEventArgs e)
        => NavigateToCommitteeReports?.Invoke();

    private async Task LoadAsync()
    {
        var hour = DateTime.Now.Hour;
        var timeOfDay = hour < 12 ? "morning" : hour < 17 ? "afternoon" : "evening";
        GreetingLabel.Text = $"Good {timeOfDay}, {_currentUser.Username}";

        try
        {
            // One server-side aggregate instead of downloading every list and counting here.
            var d = await _dashboard.GetAsync();
            var year = DateTime.Now.Year;

            // Module cards
            OrdinanceCount.Text  = d.Ordinances.Total.ToString();
            OrdinanceSub.Text    = $"{d.Ordinances.ThisYear} added in {year}";

            ResolutionCount.Text = d.Resolutions.Total.ToString();
            ResolutionSub.Text   = $"{d.Resolutions.ThisYear} added in {year}";

            ReportCount.Text     = d.CommitteeReports.Total.ToString();
            ReportSub.Text       = $"{d.CommitteeReports.ThisYear} added in {year}";

            // By status (ordinances only)
            int total    = d.Ordinances.Total;
            int inEffect = d.Ordinances.CountFor("in_effect");
            int amended  = d.Ordinances.CountFor("amended");
            int repealed = d.Ordinances.CountFor("repealed");
            int review   = d.Ordinances.CountFor("under_review");

            string Pct(int n) => total > 0 ? $"{(int)Math.Round(n * 100.0 / total)}%" : "0%";
            StatusInEffectNum.Text = inEffect.ToString();
            StatusInEffectPct.Text = Pct(inEffect);
            StatusAmendedNum.Text  = amended.ToString();
            StatusAmendedPct.Text  = Pct(amended);
            StatusRepealedNum.Text = repealed.ToString();
            StatusRepealedPct.Text = Pct(repealed);
            StatusReviewNum.Text   = review.ToString();
            StatusReviewPct.Text   = Pct(review);

            RoleTipLabel.Text = _currentUser.RoleName switch
            {
                "Admin" or "SuperAdmin" => "The sidebar shows different items per role — Admin sees Users + Audit log, Encoder sees Ordinances + Reports, Viewer only sees Dashboard + Ordinances.",
                "Encoder"               => "You can add and edit ordinances, resolutions, minutes, and committee reports from their pages.",
                _                       => "You have read-only access to the records. Contact an administrator to request changes."
            };

            // Recent activity (already merged, sorted and trimmed to 6 by the server)
            RecentActionsList.ItemsSource = d.Recent.Select(r => new RecentActionItem
            {
                Description = Describe(r),
                TimeAgo     = FormatTimeAgo(r.Date.ToDateTime(TimeOnly.MinValue))
            }).ToList();
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.HandleIfApiFailure(ex))
                return;

            MessageBox.Show($"Error loading dashboard data:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string Describe(ApiDashboardRecent r) => r.Type switch
    {
        "ordinance"        => $"📜 {r.Number} — {PrettyStatus(r.Detail)}",
        "resolution"       => $"🗳 Resolution {r.Number}",
        "committee_report" => $"🗂 {r.Number} — {r.Detail}",
        _                  => r.Number
    };

    // "in_effect" -> "In Effect"
    private static string PrettyStatus(string? status) =>
        string.IsNullOrEmpty(status)
            ? ""
            : string.Join(' ', status.Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(w => char.ToUpper(w[0]) + w[1..]));

    private static string FormatTimeAgo(DateTime dt)
    {
        var span = DateTime.Now - dt;
        if (span.TotalMinutes < 60)  return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24)    return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 30)     return $"{(int)span.TotalDays}d ago";
        if (span.TotalDays < 365)    return $"{(int)(span.TotalDays / 30)}mo ago";
        return $"{(int)(span.TotalDays / 365)}y ago";
    }

    private class RecentActionItem
    {
        public string Description { get; set; } = "";
        public string TimeAgo     { get; set; } = "";
    }
}