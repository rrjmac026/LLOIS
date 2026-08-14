namespace LLOIS.Views;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LLOIS.Models;
using LLOIS.Services;

public partial class DashboardView : UserControl
{
    private readonly IOrdinanceService       _ordinanceService;
    private readonly IResolutionService      _resolutionService;
    private readonly ICommitteeReportService _reportService;
    private readonly User                    _currentUser;

    public event Action? NavigateToOrdinances;
    public event Action? NavigateToResolutions;
    public event Action? NavigateToCommitteeReports;

    public DashboardView(
        IOrdinanceService ordinanceService,
        IResolutionService resolutionService,
        ICommitteeReportService reportService,
        User user)
    {
        InitializeComponent();
        _ordinanceService  = ordinanceService;
        _resolutionService = resolutionService;
        _reportService     = reportService;
        _currentUser       = user;
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
            var ordinancesTask = Task.Run(() => _ordinanceService.Search("").ToList());
            var resolutionsTask = Task.Run(() => _resolutionService.Search("").ToList());
            var reportsTask = Task.Run(() => _reportService.Search("").ToList());

            await Task.WhenAll(ordinancesTask, resolutionsTask, reportsTask);

            var ordinances  = ordinancesTask.Result;
            var resolutions = resolutionsTask.Result;
            var reports     = reportsTask.Result;

            int total     = ordinances.Count;
            int inEffect  = ordinances.Count(o => o.Status == OrdinanceStatus.InEffect);
            int amended   = ordinances.Count(o => o.Status == OrdinanceStatus.Amended);
            int repealed  = ordinances.Count(o => o.Status == OrdinanceStatus.Repealed);
            int review    = ordinances.Count(o => o.Status == OrdinanceStatus.UnderReview);
            int thisYear  = ordinances.Count(o => o.DatePassed?.Year == DateTime.Now.Year);

            // Module cards
            OrdinanceCount.Text  = total.ToString();
            OrdinanceSub.Text    = $"{thisYear} added in {DateTime.Now.Year}";

            ResolutionCount.Text = resolutions.Count.ToString();
            int resThisYear = resolutions.Count(r => r.DateApproved?.Year == DateTime.Now.Year);
            ResolutionSub.Text   = $"{resThisYear} added in {DateTime.Now.Year}";

            ReportCount.Text     = reports.Count.ToString();
            int repThisYear = reports.Count(r => r.Date?.Year == DateTime.Now.Year);
            ReportSub.Text        = $"{repThisYear} added in {DateTime.Now.Year}";

            // By status (ordinances only)
            string Pct(int n) => total > 0 ? $"{(int)Math.Round(n * 100.0 / total)}%" : "0%";
            StatusInEffectNum.Text = inEffect.ToString();
            StatusInEffectPct.Text = Pct(inEffect);
            StatusAmendedNum.Text  = amended.ToString();
            StatusAmendedPct.Text  = Pct(amended);
            StatusRepealedNum.Text = repealed.ToString();
            StatusRepealedPct.Text = Pct(repealed);
            StatusReviewNum.Text   = review.ToString();
            StatusReviewPct.Text   = Pct(review);

            RoleTipLabel.Text = _currentUser.Role switch
            {
                UserRole.Admin   => "The sidebar shows different items per role — Admin sees Users + Audit log, Encoder sees Ordinances + Reports, Viewer only sees Dashboard + Ordinances.",
                UserRole.Encoder => "You can add, edit, and add amendments to ordinances. Use the Ordinances page to manage records.",
                _                => "You have read-only access to the ordinances. Contact an administrator to request changes."
            };

            // Recent actions across all three modules
            var recentItems = ordinances
                .Where(o => o.DatePassed.HasValue)
                .Select(o => new RecentActionItem
                {
                    Description = $"📜 {o.OrdinanceNumber} — {o.Status}",
                    When        = o.DatePassed!.Value.ToDateTime(TimeOnly.MinValue)
                })
                .Concat(resolutions
                    .Where(r => r.DateApproved.HasValue)
                    .Select(r => new RecentActionItem
                    {
                        Description = $"🗳 Resolution {r.ResolutionNumber}",
                        When        = r.DateApproved!.Value.ToDateTime(TimeOnly.MinValue)
                    }))
                .Concat(reports
                    .Where(r => r.Date.HasValue)
                    .Select(r => new RecentActionItem
                    {
                        Description = $"🗂 {r.ReportNumber} — {r.Subject}",
                        When        = r.Date!.Value.ToDateTime(TimeOnly.MinValue)
                    }))
                .OrderByDescending(x => x.When)
                .Take(6)
                .Select(x => new RecentActionItem
                {
                    Description = x.Description,
                    TimeAgo     = FormatTimeAgo(x.When)
                })
                .ToList();

            RecentActionsList.ItemsSource = recentItems;
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                return;

            MessageBox.Show($"Error loading dashboard data:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

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
        public string   Description { get; set; } = "";
        public string   TimeAgo     { get; set; } = "";
        public DateTime When;
    }
}