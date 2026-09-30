namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using LLOIS.Services;

//Main View file

public partial class MainView : UserControl
{
    private readonly ICommitteeReportService _committeeReportService;
    private readonly IOrdinanceService       _ordinanceService;
    private readonly IResolutionService      _resolutionService;
    private readonly IMinutesService         _minutesService;
    private readonly IDashboardService       _dashboardService;
    private readonly IReportService          _reportService;
    private readonly IFeedbackService        _feedbackService;
    private readonly IAuthService            _auth;
    private readonly ApiUser                 _currentUser;
    private readonly ApiClient               _api;

    private CommitteeReportsView? _committeeReportsView;
    private OrdinancesView?       _ordinancesView;
    private ResolutionsView?      _resolutionsView;
    private MinutesView?          _minutesView;
    private AuditLogView?         _auditLogView;
    private DashboardView?        _dashboardView;
    private ReportsView?          _reportsView;
    private UserManagementView?   _usersView;
    private FeedbackView?         _feedbackView;
    private FeedbackInboxView?    _feedbackInboxView;
    private SettingsView?         _settingsView;

    public event Action? LogoutRequested;

    public MainView(ApiUser user, ApiClient api)
    {
        InitializeComponent();
        _currentUser = user;
        _api = api;

        _committeeReportService = new CommitteeReportService(api);
        _ordinanceService       = new OrdinanceService(api);
        _resolutionService      = new ResolutionService(api);
        _minutesService         = new MinutesService(api);
        _dashboardService       = new DashboardService(api);
        _reportService          = new ReportService(api);
        _feedbackService        = new FeedbackService(api);
        _auth                   = new AuthService(api);

        Loaded += OnLoaded;
        ThemeService.ThemeChanged += dark => Dispatcher.Invoke(() => SyncTheme(dark));
    }

    public void PreloadData()
    {
        if (IsLoaded) ShowCommitteeReports();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ConfigureSidebar();      // MainView.Sidebar.cs
        StartUpdateChecks();     // MainView.Updates.cs

        SyncTheme(ThemeService.IsDark);
        ShowCommitteeReports();

        ShowUpdateCompleteNotice();   // MainView.Updates.cs
    }

    // ── Page navigation ─────────────────────────────────────────

    private void ShowDashboard()
    {
        PageTitleLabel.Text = "Dashboard";
        SetNavActive("dashboard");

        if (_dashboardView is null)
        {
            _dashboardView = new DashboardView(_dashboardService, _currentUser);
            _dashboardView.NavigateToOrdinances       += ShowOrdinances;
            _dashboardView.NavigateToResolutions      += ShowResolutions;
            _dashboardView.NavigateToCommitteeReports += ShowCommitteeReports;
        }

        _dashboardView.Refresh();
        PageHost.Content = _dashboardView;
    }

    private void ShowCommitteeReports()
    {
        PageTitleLabel.Text = "Committee Reports";
        SetNavActive("committeereports");

        _committeeReportsView ??= new CommitteeReportsView(_committeeReportService, _currentUser);

        _committeeReportsView.Refresh();
        PageHost.Content = _committeeReportsView;
    }

    private void ShowOrdinances()
    {
        PageTitleLabel.Text = "Ordinances";
        SetNavActive("ordinances");

        _ordinancesView ??= new OrdinancesView(_ordinanceService, _currentUser);

        _ordinancesView.ReloadIfNeeded();
        PageHost.Content = _ordinancesView;
    }

    private void ShowResolutions()
    {
        PageTitleLabel.Text = "Resolutions";
        SetNavActive("resolutions");

        _resolutionsView ??= new ResolutionsView(_resolutionService, _auth, _currentUser);

        _resolutionsView.ReloadIfNeeded();
        PageHost.Content = _resolutionsView;
    }

    private void ShowMinutes()
    {
        PageTitleLabel.Text = "Minutes";
        SetNavActive("minutes");

        _minutesView ??= new MinutesView(_minutesService, _auth, _currentUser);

        _minutesView.ReloadIfNeeded();
        PageHost.Content = _minutesView;
    }

    private void ShowReports()
    {
        PageTitleLabel.Text = "Reports";
        SetNavActive("reports");

        _reportsView ??= new ReportsView(_reportService);

        // Reloads the active report every visit so it stays fresh.
        _reportsView.ReloadIfNeeded();
        PageHost.Content = _reportsView;
    }

    private void ShowAuditLog()
    {
        PageTitleLabel.Text = "Audit Log";
        SetNavActive("audit");

        _auditLogView ??= new AuditLogView(_auth);

        // Re-pull on every visit — logs change constantly.
        _auditLogView.Refresh();
        PageHost.Content = _auditLogView;
    }

    private void ShowUsers()
    {
        PageTitleLabel.Text = "Users";
        SetNavActive("users");

        _usersView ??= new UserManagementView(_auth);

        _usersView.ReloadIfNeeded();
        PageHost.Content = _usersView;
    }

    private void ShowFeedback()
    {
        PageTitleLabel.Text = "Feedback";
        SetNavActive("feedback");

        _feedbackView ??= new FeedbackView(_feedbackService);

        PageHost.Content = _feedbackView;
    }

    private void ShowFeedbackInbox()
    {
        PageTitleLabel.Text = "Feedback Inbox";
        SetNavActive("feedbackinbox");

        _feedbackInboxView ??= new FeedbackInboxView(_feedbackService);

        _feedbackInboxView.Refresh();
        PageHost.Content = _feedbackInboxView;
    }

    private void ShowSettings()
    {
        PageTitleLabel.Text = "Settings";
        SetNavActive("settings");   // no nav button matches, so all go inactive

        if (_settingsView is null)
            _settingsView = new SettingsView();   // checks for updates in its constructor
        else
            _settingsView.RefreshUpdateCheck();

        PageHost.Content = _settingsView;
    }
}