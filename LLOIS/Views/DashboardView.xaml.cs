namespace LLOIS.Views;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LLOIS.Models;
using LLOIS.Services;

public partial class DashboardView : UserControl
{
    private readonly IOrdinanceService _service;
    private readonly User              _currentUser;

    public event Action? NavigateToOrdinances;

    public DashboardView(IOrdinanceService service, User user)
    {
        InitializeComponent();
        _service     = service;
        _currentUser = user;
    }

    public void Refresh() => _ = LoadAsync();

    private async Task LoadAsync()
    {
        // Greeting
        var hour = DateTime.Now.Hour;
        var timeOfDay = hour < 12 ? "morning" : hour < 17 ? "afternoon" : "evening";
        GreetingLabel.Text = $"Good {timeOfDay}, {_currentUser.Username}";

        var all = await Task.Run(() => _service.Search("").ToList());
        int total     = all.Count;
        int inEffect  = all.Count(o => o.Status == OrdinanceStatus.InEffect);
        int amended   = all.Count(o => o.Status == OrdinanceStatus.Amended);
        int repealed  = all.Count(o => o.Status == OrdinanceStatus.Repealed);
        int review    = all.Count(o => o.Status == OrdinanceStatus.UnderReview);
        int thisYear  = all.Count(o => o.DatePassed?.Year == DateTime.Now.Year);

        // Stat cards
        TotalCount.Text    = total.ToString();
        InEffectCount.Text = inEffect.ToString();
        ThisYearCount.Text = thisYear.ToString();
        ThisYearLabel.Text = $"In {DateTime.Now.Year}";
        ThisYearSub.Text   = $"Added in {DateTime.Now.Year}";

        // By status
        string Pct(int n) => total > 0 ? $"{(int)Math.Round(n * 100.0 / total)}%" : "0%";
        StatusInEffectNum.Text = inEffect.ToString();
        StatusInEffectPct.Text = Pct(inEffect);
        StatusAmendedNum.Text  = amended.ToString();
        StatusAmendedPct.Text  = Pct(amended);
        StatusRepealedNum.Text = repealed.ToString();
        StatusRepealedPct.Text = Pct(repealed);
        StatusReviewNum.Text   = review.ToString();
        StatusReviewPct.Text   = Pct(review);

        // Role tip
        RoleTipLabel.Text = _currentUser.Role switch
        {
            UserRole.Admin   => "The sidebar shows different items per role — Admin sees Users + Audit log, Encoder sees Ordinances + Reports, Viewer only sees Dashboard + Ordinances.",
            UserRole.Encoder => "You can add, edit, and add amendments to ordinances. Use the Ordinances page to manage records.",
            _                => "You have read-only access to the ordinances. Contact an administrator to request changes."
        };

        // Recent actions — use audit log if admin, else show recent ordinances
        var recentItems = all
            .Where(o => o.DatePassed.HasValue)
            .OrderByDescending(o => o.DatePassed)
            .Take(5)
            .Select(o => new RecentActionItem
            {
                Description = $"{o.OrdinanceNumber} — {o.Status}",
                TimeAgo     = FormatTimeAgo(o.DatePassed!.Value.ToDateTime(TimeOnly.MinValue))
            })
            .ToList();

        RecentActionsList.ItemsSource = recentItems;
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
        public string Description { get; set; } = "";
        public string TimeAgo     { get; set; } = "";
    }
}
