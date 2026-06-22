namespace LLOIS.Views;

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LLOIS.Data;
using LLOIS.Models;
using LLOIS.Repositories;
using LLOIS.Services;

public partial class MainView : UserControl
{
    private readonly IOrdinanceService _service;
    private readonly IAuthService      _auth;
    private readonly User              _currentUser;

    // Sub-views created once and reused
    private DashboardView?   _dashboardView;
    private OrdinancesView?  _ordinancesView;

    public event Action? LogoutRequested;

    public MainView(User user, SimpleDbContextFactory dbFactory)
    {
        InitializeComponent();
        _currentUser = user;
        _service     = new OrdinanceService(new OrdinanceRepository(dbFactory));
        _auth        = new AuthService(new UserRepository(dbFactory), dbFactory);
        Loaded      += OnLoaded;

        ThemeService.ThemeChanged += dark => Dispatcher.Invoke(() => SyncTheme(dark));
    }

    // ── Startup ────────────────────────────────────────────────────────────

    public void PreloadData()
    {
        if (IsLoaded) ShowDashboard();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Populate sidebar user area
        SidebarUsernameLabel.Text = _currentUser.Username;
        SidebarRoleLabel.Text     = _currentUser.Role.ToString();
        SidebarAvatarLabel.Text   = _currentUser.Username.Length > 0
            ? _currentUser.Username[0].ToString().ToUpper() : "?";

        // Populate topbar user area
        TopbarUsernameLabel.Text = _currentUser.Username;
        DropdownNameLabel.Text   = _currentUser.Username;
        DropdownRoleLabel.Text   = _currentUser.Role.ToString();
        DropdownAvatarLabel.Text = SidebarAvatarLabel.Text;

        bool isAdmin  = _currentUser.Role == UserRole.Admin;
        AdminSectionLabel.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        NavUsersBtn.Visibility       = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        NavAuditBtn.Visibility       = isAdmin ? Visibility.Visible : Visibility.Collapsed;

        // Audit log badge
        if (isAdmin)
        {
            _ = Task.Run(() =>
            {
                var count = _auth.GetRecentLogs(50).Count();
                Dispatcher.Invoke(() =>
                {
                    if (count > 0)
                    {
                        AuditBadge.Visibility = Visibility.Visible;
                        AuditBadgeCount.Text  = count > 99 ? "99+" : count.ToString();
                    }
                });
            });
        }

        SyncTheme(ThemeService.IsDark);
        ShowDashboard();
    }

    // ── Theme ──────────────────────────────────────────────────────────────

    private void SyncTheme(bool dark)
    {
        ThemeIcon.Text = dark ? "🌙" : "☀";
        // Also update the popup label if template is applied
        if (UserPopup.Child is Border popupBorder)
        {
            // Walk the visual tree to find PopupThemeLabel
            UpdatePopupThemeLabel(popupBorder, dark);
        }
    }

    private static void UpdatePopupThemeLabel(DependencyObject parent, bool dark)
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is TextBlock tb)
            {
                if (tb.Name == "PopupThemeIcon") tb.Text  = dark ? "🌙" : "☀";
                if (tb.Name == "PopupThemeLabel") tb.Text = dark ? "Light Mode" : "Dark Mode";
            }
            UpdatePopupThemeLabel(child, dark);
        }
    }

    private void ThemeToggleBtn_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.Toggle();
        UserPopup.IsOpen = false;
        // Refresh current page to pick up new colors
        if (PageHost.Content is DashboardView dv) dv.Refresh();
    }

    // ── Sidebar navigation ─────────────────────────────────────────────────

    private void NavDashboard_Click(object sender, RoutedEventArgs e)  => ShowDashboard();
    private void NavOrdinances_Click(object sender, RoutedEventArgs e) => ShowOrdinances();
    private void NavReports_Click(object sender, RoutedEventArgs e)
        => new ReportsWindow(_service) { Owner = Window.GetWindow(this) }.ShowDialog();
    private void NavUsers_Click(object sender, RoutedEventArgs e)
        => new UserManagementWindow(_auth) { Owner = Window.GetWindow(this) }.ShowDialog();
    private void NavAudit_Click(object sender, RoutedEventArgs e)
    {
        AuditBadge.Visibility = Visibility.Collapsed;
        new AuditLogWindow(_auth) { Owner = Window.GetWindow(this) }.ShowDialog();
    }

    private void ShowDashboard()
    {
        PageTitleLabel.Text = "Dashboard";
        SetNavActive("dashboard");

        if (_dashboardView is null)
        {
            _dashboardView = new DashboardView(_service, _currentUser);
            _dashboardView.NavigateToOrdinances += ShowOrdinances;
        }

        _dashboardView.Refresh();
        PageHost.Content = _dashboardView;
    }

    private void ShowOrdinances()
    {
        PageTitleLabel.Text = "Ordinances";
        SetNavActive("ordinances");

        if (_ordinancesView is null)
        {
            _ordinancesView = new OrdinancesView(_service, _auth, _currentUser);
        }

        _ordinancesView.ReloadIfNeeded();
        PageHost.Content = _ordinancesView;
    }

    private void SetNavActive(string page)
    {
        NavDashboardBtn.Style  = page == "dashboard"  ? (Style)FindResource("SidebarNavBtnActive") : (Style)FindResource("SidebarNavBtn");
        NavOrdinancesBtn.Style = page == "ordinances" ? (Style)FindResource("SidebarNavBtnActive") : (Style)FindResource("SidebarNavBtn");
        NavReportsBtn.Style    = page == "reports"    ? (Style)FindResource("SidebarNavBtnActive") : (Style)FindResource("SidebarNavBtn");
    }

    // ── Topbar search (delegates to ordinances view) ───────────────────────

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        ShowOrdinances();
        _ordinancesView?.ApplySearch(TopSearchBox.Text.Trim());
    }

    // ── User popup ─────────────────────────────────────────────────────────

    private void UserChipBtn_Click(object sender, RoutedEventArgs e)
        => UserPopup.IsOpen = !UserPopup.IsOpen;

    // ── Logout ─────────────────────────────────────────────────────────────

    private async void LogoutBtn_Click(object sender, RoutedEventArgs e)
    {
        UserPopup.IsOpen = false;
        var result = MessageBox.Show("Are you sure you want to log out?",
            "Logout", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;
        _ = Task.Run(() => _auth.LogAction(_currentUser, "LOGOUT",
            $"{_currentUser.Username} logged out."));
        LogoutRequested?.Invoke();
    }
}
