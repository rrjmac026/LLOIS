namespace LLOIS;

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LLOIS.Data;
using LLOIS.Models;
using LLOIS.Repositories;
using LLOIS.Services;
using LLOIS.Views;

public partial class MainWindow : Window
{
    private readonly IOrdinanceService _service;
    private readonly IAuthService _auth;
    private readonly AppDbContext _db;
    private readonly User _currentUser;
    private string _searchQuery = string.Empty;
    private Ordinance? _selectedOrdinance;

    public MainWindow()
    {
        throw new InvalidOperationException("Use MainWindow(User, AppDbContext) instead.");
    }

    public MainWindow(User user, AppDbContext db)
    {
        InitializeComponent();
        _currentUser = user;
        _db = db;
        _service = new OrdinanceService(new OrdinanceRepository(_db));
        _auth = new AuthService(new UserRepository(_db), _db);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Title = $"LLOIS — {_currentUser.Username} ({_currentUser.Role})";
        UserLabel.Text = $"{_currentUser.Username} · {_currentUser.Role}";

        // Role-based UI
        bool isAdmin   = _currentUser.Role == UserRole.Admin;
        bool canWrite  = _currentUser.Role is UserRole.Admin or UserRole.Encoder;

        AddBtn.Visibility    = canWrite ? Visibility.Visible  : Visibility.Collapsed;
        UsersBtn.Visibility  = isAdmin  ? Visibility.Visible  : Visibility.Collapsed;
        AuditBtn.Visibility  = isAdmin  ? Visibility.Visible  : Visibility.Collapsed;

        LoadOrdinances();
    }

    // ─── Load / Search ────────────────────────────────────────────────────────

    private void LoadOrdinances()
    {
        if (_service is null) return;

        try
        {
            var results = _service.Search(_searchQuery).ToList();

            if (StatusFilter.SelectedItem is ComboBoxItem { Content: string status } && status != "All"
                && Enum.TryParse<OrdinanceStatus>(status.Replace(" ", ""), out var parsed))
            {
                results = results.Where(o => o.Status == parsed).ToList();
            }

            OrdinanceList.ItemsSource = results;
            ResultCount.Text = $"{results.Count} ordinance(s) found";
            ClearDetail();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading ordinances:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        _searchQuery = SearchBox.Text.Trim();
        if (_searchQuery == "Search by ID, title, subject, sponsor...") _searchQuery = "";
        LoadOrdinances();
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) SearchButton_Click(sender, e);
    }

    private void StatusFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_service is null) return; // ← add this
        LoadOrdinances();
    }
    

    private void OrdinanceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OrdinanceList.SelectedItem is not Ordinance o) return;
        var detail = _service.GetDetails(o.OrdinanceNumber);
        if (detail is null) return;
        _selectedOrdinance = detail;
        ShowDetail(detail);
    }

    // ─── Toolbar Buttons ──────────────────────────────────────────────────────

    private void AddBtn_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AddEditOrdinanceWindow(_service) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            _auth.LogAction(_currentUser, "ADD", $"Added ordinance {dlg.SavedOrdinance?.OrdinanceNumber}");
            LoadOrdinances();
            MessageBox.Show("Ordinance added successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void EditBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedOrdinance is null) return;
        var dlg = new AddEditOrdinanceWindow(_service, _selectedOrdinance) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            _auth.LogAction(_currentUser, "EDIT", $"Edited ordinance {_selectedOrdinance.OrdinanceNumber}");
            LoadOrdinances();
            // Re-select and refresh detail
            var updated = _service.GetDetails(_selectedOrdinance.OrdinanceNumber);
            if (updated is not null) { _selectedOrdinance = updated; ShowDetail(updated); }
        }
    }

    private void AmendBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedOrdinance is null) return;
        var dlg = new AddAmendmentWindow(_service, _selectedOrdinance) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            _auth.LogAction(_currentUser, "AMEND", $"Added amendment to {_selectedOrdinance.OrdinanceNumber}");
            var updated = _service.GetDetails(_selectedOrdinance.OrdinanceNumber);
            if (updated is not null) { _selectedOrdinance = updated; ShowDetail(updated); }
            LoadOrdinances();
        }
    }

    private void OpenPdfBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedOrdinance?.DocumentPath is null) return;

        if (!System.IO.File.Exists(_selectedOrdinance.DocumentPath))
        {
            MessageBox.Show("The attached PDF file could not be found at:\n" +
                            _selectedOrdinance.DocumentPath,
                "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(_selectedOrdinance.DocumentPath) { UseShellExecute = true });
            _auth.LogAction(_currentUser, "OPEN_PDF", $"Opened PDF for {_selectedOrdinance.OrdinanceNumber}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open PDF:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedOrdinance is null) return;

        var result = MessageBox.Show(
            $"Permanently delete ordinance {_selectedOrdinance.OrdinanceNumber}?\n\n" +
            "This will also delete all its versions. This cannot be undone.",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            var num = _selectedOrdinance.OrdinanceNumber;
            _service.Delete(num);
            _auth.LogAction(_currentUser, "DELETE", $"Deleted ordinance {num}");
            LoadOrdinances();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Delete failed:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ReportsBtn_Click(object sender, RoutedEventArgs e)
    {
        new ReportsWindow(_service) { Owner = this }.ShowDialog();
    }

    private void UsersBtn_Click(object sender, RoutedEventArgs e)
    {
        new UserManagementWindow(_auth) { Owner = this }.ShowDialog();
    }

    private void AuditBtn_Click(object sender, RoutedEventArgs e)
    {
        new AuditLogWindow(_auth) { Owner = this }.ShowDialog();
    }

    // ─── Detail Panel ─────────────────────────────────────────────────────────

    private void ShowDetail(Ordinance o)
    {
        DetailPanel.Visibility = Visibility.Visible;
        ActionBar.Visibility   = Visibility.Visible;

        // Role-gated action buttons
        bool canWrite = _currentUser.Role is UserRole.Admin or UserRole.Encoder;
        bool isAdmin  = _currentUser.Role == UserRole.Admin;

        EditBtn.Visibility   = canWrite  ? Visibility.Visible : Visibility.Collapsed;
        AmendBtn.Visibility  = canWrite  ? Visibility.Visible : Visibility.Collapsed;
        DeleteBtn.Visibility = isAdmin   ? Visibility.Visible : Visibility.Collapsed;
        OpenPdfBtn.Visibility = o.DocumentPath is not null ? Visibility.Visible : Visibility.Collapsed;

        // Header
        DetailId.Text      = o.OrdinanceNumber;
        DetailSubject.Text = o.Subject;
        DetailSeries.Text  = $"{o.SeriesNumber}  ·  {o.Type}  ·  Sponsor: {o.Sponsor}";
        DetailStatus.Text  = o.Status.ToString();

        (StatusBadgeControl.Background, StatusBadgeControl.BorderBrush) = o.Status switch
        {
            OrdinanceStatus.InEffect   => (Brushes.LightGreen,  Brushes.Green),
            OrdinanceStatus.Amended    => (Brushes.LightYellow, Brushes.Orange),
            OrdinanceStatus.Superseded => (Brushes.LightBlue,   Brushes.SteelBlue),
            OrdinanceStatus.Repealed   => (Brushes.LightCoral,  Brushes.Red),
            OrdinanceStatus.UnderReview=> (Brushes.LightGray,   Brushes.DarkGray),
            _ => (Brushes.LightGray, Brushes.Gray)
        };
        StatusBadgeControl.BorderThickness = new Thickness(1);

        // Relationship badges
        bool hasAny = false;
        if (!string.IsNullOrEmpty(o.AmendsOrdinanceNumber))
        {
            AmendsLabel.Text    = $"Amends: {o.AmendsOrdinanceNumber}";
            AmendsPanel.Visibility = Visibility.Visible; hasAny = true;
        } else AmendsPanel.Visibility = Visibility.Collapsed;

        if (!string.IsNullOrEmpty(o.SupersedesOrdinanceNumber))
        {
            SupersedesLabel.Text    = $"Supersedes: {o.SupersedesOrdinanceNumber}";
            SupersedesPanel.Visibility = Visibility.Visible; hasAny = true;
        } else SupersedesPanel.Visibility = Visibility.Collapsed;

        if (!string.IsNullOrEmpty(o.RepealedByOrdinanceNumber))
        {
            RepealedByLabel.Text    = $"Repealed by: {o.RepealedByOrdinanceNumber}";
            RepealedByPanel.Visibility = Visibility.Visible; hasAny = true;
        } else RepealedByPanel.Visibility = Visibility.Collapsed;

        RelationshipPanel.Visibility = hasAny ? Visibility.Visible : Visibility.Collapsed;
        PdfBadge.Visibility = !string.IsNullOrEmpty(o.DocumentPath) ? Visibility.Visible : Visibility.Collapsed;

        // Metadata columns
        MetaLeft.Children.Clear();
        MetaRight.Children.Clear();
        MetaLeft.Children.Add(MakeRow("Committee",    o.Committee));
        MetaLeft.Children.Add(MakeRow("Date Passed",  o.DatePassed?.ToString("MMMM dd, yyyy") ?? "—"));
        MetaLeft.Children.Add(MakeRow("Mayor Approved", o.DateApprovedByMayor?.ToString("MMMM dd, yyyy") ?? "—"));
        MetaRight.Children.Add(MakeRow("Date Published", o.DatePublished?.ToString("MMMM dd, yyyy") ?? "—"));
        if (!string.IsNullOrEmpty(o.RepealReason))
            MetaRight.Children.Add(MakeRow("Repeal Reason", o.RepealReason, color: "#8b1a1a"));

        // Latest version
        LatestVersionPanel.Children.Clear();
        if (o.LatestVersion is OrdinanceVersion latest)
        {
            LatestVersionPanel.Children.Add(MakeRow("Title",        latest.Title, bold: true));
            LatestVersionPanel.Children.Add(MakeRow("Date Enacted", latest.DateEnacted.ToString("MMMM dd, yyyy")));
            LatestVersionPanel.Children.Add(MakeRow("Enacted By",   latest.EnactedBy));
            LatestVersionPanel.Children.Add(MakeRow("Content",      latest.Content));
            if (!string.IsNullOrEmpty(latest.AmendmentNotes))
                LatestVersionPanel.Children.Add(MakeRow("Notes", latest.AmendmentNotes, color: "#CC6600"));
        }

        // Version history (all except latest)
        var history = o.Versions.OrderBy(v => v.VersionNumber).SkipLast(1).ToList();
        HistoryHeader.Visibility   = history.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        VersionHistoryList.ItemsSource = history;

        _auth.LogAction(_currentUser, "VIEW", $"Viewed ordinance {o.OrdinanceNumber}");
    }

    private void ClearDetail()
    {
        DetailPanel.Visibility = Visibility.Collapsed;
        ActionBar.Visibility   = Visibility.Collapsed;
        _selectedOrdinance     = null;
    }

    private static StackPanel MakeRow(string label, string value,
        bool bold = false, string? color = null)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 2, 0, 2)
        };
        panel.Children.Add(new TextBlock
        {
            Text = label + ": ",
            FontWeight = FontWeights.SemiBold,
            FontSize = 12,
            MinWidth = 110
        });
        panel.Children.Add(new TextBlock
        {
            Text = value,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Foreground = color is null
                ? Brushes.Black
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
            MaxWidth = 380
        });
        return panel;
    }
}
