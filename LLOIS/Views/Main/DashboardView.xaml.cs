namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using LLOIS.Services;

public partial class DashboardView : UserControl
{
    private readonly IDashboardService _dashboard;
    private readonly ApiUser           _currentUser;
    private readonly DispatcherTimer   _timer;
    private bool _loadedOnce;
    private bool _refreshing;

    public event Action? NavigateToOrdinances;
    public event Action? NavigateToResolutions;
    public event Action? NavigateToCommitteeReports;
    public event Action? NavigateToMinutes;   // optional: hook up in MainView if you want the card clickable
    public event Action? NavigateToUsers;     // optional: same

    public DashboardView(IDashboardService dashboard, ApiUser user)
    {
        InitializeComponent();
        _dashboard   = dashboard;
        _currentUser = user;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _timer.Tick += async (_, _) => await LoadAsync(silent: true);

        // Poll only while the dashboard is actually on screen.
        IsVisibleChanged += OnVisibleChanged;
        Unloaded += (_, _) => _timer.Stop();
    }

    private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            _timer.Start();
            if (_loadedOnce) _ = LoadAsync(silent: true); // catch up right away when coming back
        }
        else
        {
            _timer.Stop();
        }
    }

    public void Refresh() => _ = LoadAsync();

    private void OrdinancesCard_Click(object sender, MouseButtonEventArgs e)
        => NavigateToOrdinances?.Invoke();

    private void ResolutionsCard_Click(object sender, MouseButtonEventArgs e)
        => NavigateToResolutions?.Invoke();

    private void CommitteeReportsCard_Click(object sender, MouseButtonEventArgs e)
        => NavigateToCommitteeReports?.Invoke();

    private void MinutesCard_Click(object sender, MouseButtonEventArgs e)
        => NavigateToMinutes?.Invoke();

    private void UsersCard_Click(object sender, MouseButtonEventArgs e)
        => NavigateToUsers?.Invoke();

    private async Task LoadAsync(bool silent = false)
    {
        if (_refreshing) return; // don't stack requests if the server is slow
        _refreshing = true;

        var hour = DateTime.Now.Hour;
        var timeOfDay = hour < 12 ? "morning" : hour < 17 ? "afternoon" : "evening";
        GreetingLabel.Text = $"Good {timeOfDay}, {_currentUser.Username}";

        try
        {
            // One server-side aggregate instead of downloading every list and counting here.
            var d = await _dashboard.GetAsync();
            var year = DateTime.Now.Year;

            // ── Module cards ──
            OrdinanceCount.Text  = d.Ordinances.Total.ToString();
            OrdinanceSub.Text    = $"{d.Ordinances.ThisYear} added in {year}";

            ResolutionCount.Text = d.Resolutions.Total.ToString();
            ResolutionSub.Text   = $"{d.Resolutions.ThisYear} added in {year}";

            ReportCount.Text     = d.CommitteeReports.Total.ToString();
            ReportSub.Text       = $"{d.CommitteeReports.ThisYear} added in {year}";

            MinutesCount.Text    = d.Minutes.Total.ToString();
            MinutesSub.Text      = $"{d.Minutes.ThisYear} added in {year}";

            // Users card is only sent to Admin and above; the server sends null for everyone else.
            if (d.Users is not null)
            {
                UsersCard.Visibility = Visibility.Visible;
                UsersCount.Text = d.Users.Total.ToString();
                UsersSub.Text   = $"{d.Users.Active} active accounts";
            }
            else
            {
                UsersCard.Visibility = Visibility.Collapsed;
            }

            // ── By status (ordinances only) ──
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

            // ── Recent activity (from the audit log) ──
            // Admin+ see everyone's activity; everyone else sees only their own.
            var everyone = d.ActivityScope == "all";
            RecentTitle.Text = everyone ? "Recent activity" : "Your recent activity";

            var items = d.RecentActivity.Select(a => new RecentActionItem
            {
                Description = BuildDescription(a, everyone),
                TimeAgo     = FormatTimeAgo(ToLocal(a.CreatedAt))
            }).ToList();

            RecentActionsList.ItemsSource = items;
            EmptyActivityLabel.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            _loadedOnce = true;
        }
        catch (Exception ex)
        {
            // Background polls: ignore connection blips quietly instead of bouncing to login.
            if (silent && ConnectionFailureHandler.IsConnectionFailure(ex))
                return;

            if (ConnectionFailureHandler.HandleIfApiFailure(ex))
                return;

            // Only manual loads show an error popup; polls stay quiet.
            if (!silent)
                MessageBox.Show($"Error loading dashboard data:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _refreshing = false;
        }
    }

    private static string BuildDescription(ApiDashboardActivity a, bool everyone)
    {
        var text = everyone ? $"{a.Username} · {a.Action}" : a.Action;
        return string.IsNullOrEmpty(a.Source) ? text : $"{text}  [{a.Source}]";
    }

    // Laravel sends UTC ("...Z"). If the kind ever comes back Unspecified, treat it as UTC.
    private static DateTime? ToLocal(DateTime? dt)
    {
        if (dt is null) return null;

        var value = dt.Value;
        if (value.Kind == DateTimeKind.Unspecified)
            value = DateTime.SpecifyKind(value, DateTimeKind.Utc);

        return value.ToLocalTime();
    }

    private static string FormatTimeAgo(DateTime? dt)
    {
        if (dt is null) return "";

        var span = DateTime.Now - dt.Value;
        if (span.TotalMinutes < 1)   return "just now";
        if (span.TotalMinutes < 60)  return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24)    return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 30)     return $"{(int)(span.TotalDays / 30)}d ago".Replace($"{(int)(span.TotalDays / 30)}d", $"{(int)span.TotalDays}d");
        if (span.TotalDays < 365)    return $"{(int)(span.TotalDays / 30)}mo ago";
        return $"{(int)(span.TotalDays / 365)}y ago";
    }

    private class RecentActionItem
    {
        public string Description { get; set; } = "";
        public string TimeAgo     { get; set; } = "";
    }
}