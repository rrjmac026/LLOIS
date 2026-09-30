namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using LLOIS.Services;

//main View Sidebar file
public partial class MainView
{
    // ── Setup ───────────────────────────────────────────────────

    private void ConfigureSidebar()
    {
        UserChipBtn.ApplyTemplate();

        var avatar   = UserChipBtn.Template.FindName("SidebarAvatarLabel",   UserChipBtn) as TextBlock;
        var username = UserChipBtn.Template.FindName("SidebarUsernameLabel", UserChipBtn) as TextBlock;
        var role     = UserChipBtn.Template.FindName("SidebarRoleLabel",     UserChipBtn) as TextBlock;

        string initial = _currentUser.Username.Length > 0
            ? _currentUser.Username[0].ToString().ToUpper() : "?";

        if (avatar   != null) avatar.Text   = initial;
        if (username != null) username.Text = _currentUser.Username;
        if (role     != null) role.Text     = _currentUser.RoleName;

        DropdownNameLabel.Text   = _currentUser.Username;
        DropdownRoleLabel.Text   = _currentUser.RoleName;
        DropdownAvatarLabel.Text = initial;

        ConfigureNavVisibility();
    }

    /// <summary>Single place that decides which nav buttons show, per role.</summary>
    private void ConfigureNavVisibility()
    {
        bool isAdmin      = _currentUser.RoleName is "Admin" or "SuperAdmin";
        bool isEncoderUp  = _currentUser.RoleName is "Encoder" or "Admin" or "SuperAdmin";
        bool isSuperAdmin = _currentUser.RoleName == "SuperAdmin";

        // The API's users and audit-logs endpoints are admin-only.
        AdminSectionLabel.Visibility   = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        NavAuditBtn.Visibility         = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        NavUsersBtn.Visibility         = isAdmin ? Visibility.Visible : Visibility.Collapsed;   // ✅ NEW

        NavDashboardBtn.Visibility     = Visibility.Visible;
        NavOrdinancesBtn.Visibility    = Visibility.Visible;
        NavResolutionsBtn.Visibility   = Visibility.Visible;
        NavMinutesBtn.Visibility       = Visibility.Visible;
        NavReportsBtn.Visibility       = Visibility.Visible;

        // Feedback endpoints sit behind role:encoder; the inbox shows everyone's feedback,
        // which the API only returns to SuperAdmin.
        NavFeedbackBtn.Visibility      = isEncoderUp  ? Visibility.Visible : Visibility.Collapsed;
        NavFeedbackInboxBtn.Visibility = isSuperAdmin ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── Nav clicks ──────────────────────────────────────────────

    private void NavDashboard_Click(object sender, RoutedEventArgs e)        => ShowDashboard();
    private void NavCommitteeReports_Click(object sender, RoutedEventArgs e) => ShowCommitteeReports();
    private void NavOrdinances_Click(object sender, RoutedEventArgs e)       => ShowOrdinances();
    private void NavResolutions_Click(object sender, RoutedEventArgs e)      => ShowResolutions();
    private void NavMinutes_Click(object sender, RoutedEventArgs e)          => ShowMinutes();
    private void NavReports_Click(object sender, RoutedEventArgs e)          => ShowReports();
    private void NavAudit_Click(object sender, RoutedEventArgs e)            => ShowAuditLog();
    private void NavFeedback_Click(object sender, RoutedEventArgs e)         => ShowFeedback();
    private void NavFeedbackInbox_Click(object sender, RoutedEventArgs e)    => ShowFeedbackInbox();
    private void NavUsers_Click(object sender, RoutedEventArgs e)            => ShowUsers();   // ✅ NEW

    private void SetNavActive(string page)
    {
        Style Active()   => (Style)FindResource("SidebarNavBtnActive");
        Style Inactive() => (Style)FindResource("SidebarNavBtn");

        NavDashboardBtn.Style        = page == "dashboard"        ? Active() : Inactive();
        NavCommitteeReportsBtn.Style = page == "committeereports" ? Active() : Inactive();
        NavOrdinancesBtn.Style       = page == "ordinances"       ? Active() : Inactive();
        NavResolutionsBtn.Style      = page == "resolutions"      ? Active() : Inactive();
        NavMinutesBtn.Style          = page == "minutes"          ? Active() : Inactive();
        NavReportsBtn.Style          = page == "reports"          ? Active() : Inactive();
        NavAuditBtn.Style            = page == "audit"            ? Active() : Inactive();
        NavFeedbackBtn.Style         = page == "feedback"         ? Active() : Inactive();
        NavFeedbackInboxBtn.Style    = page == "feedbackinbox"    ? Active() : Inactive();
        NavUsersBtn.Style            = page == "users"            ? Active() : Inactive();   // ✅ NEW
    }

    // ── User popup ──────────────────────────────────────────────

    private void UserChipBtn_Click(object sender, RoutedEventArgs e)
        => UserPopup.IsOpen = !UserPopup.IsOpen;

    private void SettingsBtn_Click(object sender, RoutedEventArgs e)
    {
        UserPopup.IsOpen = false;
        ShowSettings();
    }

    private void LogoutBtn_Click(object sender, RoutedEventArgs e)
    {
        UserPopup.IsOpen = false;
        var result = MessageBox.Show("Are you sure you want to log out?",
            "Logout", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        _auth.Logout();
        LogoutRequested?.Invoke();
    }

    // ── Theme ───────────────────────────────────────────────────

    private void ThemeToggleBtn_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.Toggle();
        UserPopup.IsOpen = false;
    }

    private void SyncTheme(bool dark)
    {
        ThemeIcon.Text = dark ? "🌙" : "☀";
        if (UserPopup.Child is Border popupBorder)
            UpdatePopupThemeLabel(popupBorder, dark);
    }

    private static void UpdatePopupThemeLabel(DependencyObject parent, bool dark)
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is TextBlock tb)
            {
                if (tb.Name == "PopupThemeIcon")  tb.Text = dark ? "🌙" : "☀";
                if (tb.Name == "PopupThemeLabel") tb.Text = dark ? "Light Mode" : "Dark Mode";
            }
            UpdatePopupThemeLabel(child, dark);
        }
    }
}