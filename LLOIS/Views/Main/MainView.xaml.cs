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
    private readonly IOrdinanceService       _service;
    private readonly ICommitteeReportService _committeeReportService;
    private readonly IResolutionService      _resolutionService;
    private readonly IAuthService            _auth;
    private readonly User                    _currentUser;
    private readonly SimpleDbContextFactory  _dbFactory;   // ADD THIS
    private readonly IMinutesService        _minutesService;
    private readonly IFeedbackService       _feedbackService;

    // Sub-views created once and reused
    private DashboardView?        _dashboardView;
    private OrdinancesView?       _ordinancesView;
    private CommitteeReportsView? _committeeReportsView;
    private ResolutionsView?      _resolutionsView;
    private ReportsView?          _reportsView;
    private UserManagementView?   _usersView;
    private AuditLogView?         _auditLogView;
    private SettingsView?         _settingsView;
    private MinutesView?          _minutesView;
    private FeedbackView?      _feedbackView;
    private FeedbackInboxView? _feedbackInboxView;

    private System.Windows.Threading.DispatcherTimer? _updateCheckTimer;

    public event Action? LogoutRequested;
    

    public MainView(User user, SimpleDbContextFactory dbFactory)
    {
        InitializeComponent();
        _currentUser = user;
        _dbFactory   = dbFactory;
        _service                = new OrdinanceService(new OrdinanceRepository(dbFactory));
        _committeeReportService = new CommitteeReportService(new CommitteeReportRepository(dbFactory));
        _resolutionService      = new ResolutionService(new ResolutionRepository(dbFactory));
        _minutesService         = new MinutesService(new MinutesRepository(dbFactory));
        _auth                   = new AuthService(new UserRepository(dbFactory), dbFactory);
        _dashboardView          = new DashboardView(_service, _resolutionService, _committeeReportService, _currentUser);
        _feedbackService        = new FeedbackService(new FeedbackRepository(dbFactory));
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
        // Populate sidebar user area (via template FindName)
        UserChipBtn.ApplyTemplate();

        var avatar   = UserChipBtn.Template.FindName("SidebarAvatarLabel",   UserChipBtn) as TextBlock;
        var username = UserChipBtn.Template.FindName("SidebarUsernameLabel", UserChipBtn) as TextBlock;
        var role     = UserChipBtn.Template.FindName("SidebarRoleLabel",     UserChipBtn) as TextBlock;
        bool isSuperAdmin = _currentUser.Role == UserRole.SuperAdmin;

        string initial = _currentUser.Username.Length > 0
            ? _currentUser.Username[0].ToString().ToUpper() : "?";

        if (avatar   != null) avatar.Text   = initial;
        if (username != null) username.Text = _currentUser.Username;
        if (role     != null) role.Text     = _currentUser.Role.ToString();

        // Populate dropdown (now triggered only from the sidebar user chip)
        DropdownNameLabel.Text   = _currentUser.Username;
        DropdownRoleLabel.Text   = _currentUser.Role.ToString();
        DropdownAvatarLabel.Text = initial;

        bool isAdmin = _currentUser.Role is UserRole.Admin or UserRole.SuperAdmin;
        AdminSectionLabel.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        NavUsersBtn.Visibility       = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        NavAuditBtn.Visibility       = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        NavFeedbackInboxBtn.Visibility = isSuperAdmin ? Visibility.Visible : Visibility.Collapsed;

        // Audit log badge
        if (isAdmin)
        {
            _ = Task.Run(() =>
            {
                try
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
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex));
                }
            });
        }

        CheckForUpdateAndUpdateBadge(); // initial check

        _updateCheckTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(15)
        };
        _updateCheckTimer.Tick += (s, args) => CheckForUpdateAndUpdateBadge();
        _updateCheckTimer.Start();

        SyncTheme(ThemeService.IsDark);
        ShowDashboard();

        // Show a one-time confirmation if we just relaunched from an update
        var previousVersion = UpdateService.ConsumeUpdateMarker();
        if (previousVersion is not null)
        {
            MessageBox.Show(
                $"DLIS was successfully updated to version {App.CurrentVersion}.",
                "Update Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void CheckForUpdateAndUpdateBadge()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var update = await UpdateService.CheckForUpdateAsync();
                Dispatcher.Invoke(() =>
                {
                    SettingsBtnRef.ApplyTemplate();
                    var settingsBadge = SettingsBtnRef.Template.FindName("UpdateBadge", SettingsBtnRef) as Border;
                    if (settingsBadge is not null)
                        settingsBadge.Visibility = update is not null ? Visibility.Visible : Visibility.Collapsed;
                });
            }
            catch
            {
                // Silent — offline is fine, just skip this check cycle
            }
        });
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
    private void NavCommitteeReports_Click(object sender, RoutedEventArgs e) => ShowCommitteeReports();
    private void NavResolutions_Click(object sender, RoutedEventArgs e) => ShowResolutions();
    private void NavMinutes_Click(object sender, RoutedEventArgs e) => ShowMinutes();
    private void NavReports_Click(object sender, RoutedEventArgs e)    => ShowReports();
    private void NavUsers_Click(object sender, RoutedEventArgs e)      => ShowUsers();
    private void NavAudit_Click(object sender, RoutedEventArgs e)      => ShowAuditLog();
    private void NavFeedback_Click(object sender, RoutedEventArgs e) => ShowFeedback();
    private void NavFeedbackInbox_Click(object sender, RoutedEventArgs e) => ShowFeedbackInbox();

    private void ShowDashboard()
    {
        PageTitleLabel.Text = "Dashboard";
        SetNavActive("dashboard");

        if (_dashboardView is null)
        {
            _dashboardView = new DashboardView(_service, _resolutionService, _committeeReportService, _currentUser);
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

    private void ShowReports()
    {
        PageTitleLabel.Text = "Reports";
        SetNavActive("reports");

        if (_reportsView is null)
        {
            _reportsView = new ReportsView(_service, _auth, _currentUser);
        }

        _reportsView.ReloadIfNeeded();
        PageHost.Content = _reportsView;
    }

    private void ShowUsers()
    {
        PageTitleLabel.Text = "Users";
        SetNavActive("users");

        if (_usersView is null)
        {
            _usersView = new UserManagementView(_auth);
        }

        _usersView.ReloadIfNeeded();
        PageHost.Content = _usersView;
    }

    private void ShowAuditLog()
    {
        PageTitleLabel.Text = "Audit Log";
        SetNavActive("audit");
        AuditBadge.Visibility = Visibility.Collapsed;

        if (_auditLogView is null)
        {
            _auditLogView = new AuditLogView(_auth);
        }

        _auditLogView.Refresh(); // always pull fresh data when navigating here
        PageHost.Content = _auditLogView;
    }

    private void ShowCommitteeReports()
    {
        PageTitleLabel.Text = "Committee Reports";
        SetNavActive("committeereports");

        if (_committeeReportsView is null)
        {
            _committeeReportsView = new CommitteeReportsView(_committeeReportService, _auth, _currentUser);
        }

        _committeeReportsView.Refresh();   // always pull fresh data when navigating here
        PageHost.Content = _committeeReportsView;
    }

    private void ShowResolutions()
    {
        PageTitleLabel.Text = "Resolutions";
        SetNavActive("resolutions");

        if (_resolutionsView is null)
        {
            _resolutionsView = new ResolutionsView(_resolutionService, _auth, _currentUser);
        }

        _resolutionsView.Refresh();
        PageHost.Content = _resolutionsView;
    }

    private void ShowMinutes()
    {
        PageTitleLabel.Text = "Minutes";
        SetNavActive("minutes");

        if (_minutesView is null)
        {
            _minutesView = new MinutesView(_minutesService, _auth, _currentUser);
        }

        _minutesView.ReloadIfNeeded();
        PageHost.Content = _minutesView;
    }

    private void SetNavActive(string page)
    {
        NavDashboardBtn.Style         = page == "dashboard"         ? (Style)FindResource("SidebarNavBtnActive") : (Style)FindResource("SidebarNavBtn");
        NavOrdinancesBtn.Style        = page == "ordinances"        ? (Style)FindResource("SidebarNavBtnActive") : (Style)FindResource("SidebarNavBtn");
        NavCommitteeReportsBtn.Style  = page == "committeereports"  ? (Style)FindResource("SidebarNavBtnActive") : (Style)FindResource("SidebarNavBtn");
        NavResolutionsBtn.Style       = page == "resolutions"       ? (Style)FindResource("SidebarNavBtnActive") : (Style)FindResource("SidebarNavBtn"); // NEW
        NavReportsBtn.Style           = page == "reports"           ? (Style)FindResource("SidebarNavBtnActive") : (Style)FindResource("SidebarNavBtn");
        NavUsersBtn.Style             = page == "users"             ? (Style)FindResource("SidebarNavBtnActive") : (Style)FindResource("SidebarNavBtn");
        NavAuditBtn.Style             = page == "audit"             ? (Style)FindResource("SidebarNavBtnActive") : (Style)FindResource("SidebarNavBtn");
        NavMinutesBtn.Style           = page == "minutes"           ? (Style)FindResource("SidebarNavBtnActive") : (Style)FindResource("SidebarNavBtn");
        NavFeedbackBtn.Style       = page == "feedback"       ? (Style)FindResource("SidebarNavBtnActive") : (Style)FindResource("SidebarNavBtn");
        NavFeedbackInboxBtn.Style  = page == "feedbackinbox"   ? (Style)FindResource("SidebarNavBtnActive") : (Style)FindResource("SidebarNavBtn");
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
        _ = Task.Run(() =>
        {
            try
            {
                _auth.LogAction(_currentUser, "LOGOUT", $"{_currentUser.Username} logged out.");
            }
            catch (Exception ex)
            {
                ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex);
            }
        });
        LogoutRequested?.Invoke();
    }

    private async void CheckUpdateBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var update = await UpdateService.CheckForUpdateAsync();
            if (update is null)
            {
                MessageBox.Show($"You're on the latest version ({App.CurrentVersion}).",
                    "No Updates", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"A new version ({update.Version}) is available. Update now?\n\nThe app will restart.",
                "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result != MessageBoxResult.Yes) return;

            var progressWindow = new UpdateProgressWindow { Owner = Window.GetWindow(this) };
            progressWindow.Show();

            var progress = new Progress<double>(percent => progressWindow.SetProgress(percent));

            var path = await UpdateService.DownloadUpdateAsync(update, progress);

            progressWindow.SetStatus("Restarting...");
            await Task.Delay(500); // brief pause so "Restarting..." is visible

            UpdateService.ApplyUpdateAndRestart(path);
        }
        catch (Exception ex)
        {
            if (!ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                MessageBox.Show($"Update check failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private void SettingsBtn_Click(object sender, RoutedEventArgs e)
    {
        UserPopup.IsOpen = false;

        SettingsBtnRef.ApplyTemplate();
        var settingsBadge = SettingsBtnRef.Template.FindName("UpdateBadge", SettingsBtnRef) as Border;
        if (settingsBadge is not null)
            settingsBadge.Visibility = Visibility.Collapsed;

        PageTitleLabel.Text = "Settings";
        SetNavActive("settings");

        if (_settingsView is null)
        {
            _settingsView = new SettingsView(_currentUser, _auth, _dbFactory);
        }
        else
        {
            _settingsView.RefreshUpdateCheck(); // re-check every time Settings is opened
        }

        PageHost.Content = _settingsView;
    }

    private void ShowFeedback()
    {
        PageTitleLabel.Text = "Feedback";
        SetNavActive("feedback");

        if (_feedbackView is null)
            _feedbackView = new FeedbackView(_feedbackService, _currentUser);

        PageHost.Content = _feedbackView;
    }

    private void ShowFeedbackInbox()
    {
        PageTitleLabel.Text = "Feedback Inbox";
        SetNavActive("feedbackinbox");

        if (_feedbackInboxView is null)
            _feedbackInboxView = new FeedbackInboxView(_feedbackService);

        _feedbackInboxView.Refresh();
        PageHost.Content = _feedbackInboxView;
    }
}
