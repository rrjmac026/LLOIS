namespace LLOIS.Views;

using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LLOIS.Data;
using LLOIS.Models;
using LLOIS.Repositories;
using LLOIS.Services;

public partial class MainView : UserControl
{
    private readonly IOrdinanceService _service;
    private readonly IAuthService      _auth;
    private readonly User              _currentUser;
    private string    _searchQuery      = string.Empty;
    private Ordinance? _selectedOrdinance;

    public event Action? LogoutRequested;

    public MainView(User user, SimpleDbContextFactory dbFactory)
    {
        InitializeComponent();
        _currentUser = user;
        _service     = new OrdinanceService(new OrdinanceRepository(dbFactory));
        _auth        = new AuthService(new UserRepository(dbFactory), dbFactory);
        Loaded      += OnLoaded;

        // Keep theme-toggle button label in sync
        ThemeService.ThemeChanged += dark =>
            Dispatcher.Invoke(() => ThemeToggleBtn.Content = dark ? "☀ Light" : "🌙 Dark");
    }

    public void PreloadData()
    {
        if (IsLoaded) _ = LoadOrdinancesAsync();
    }

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        UserLabel.Text = $"{_currentUser.Username} · {_currentUser.Role}";

        bool isAdmin  = _currentUser.Role == UserRole.Admin;
        bool canWrite = _currentUser.Role is UserRole.Admin or UserRole.Encoder;

        AddBtn.Visibility   = canWrite ? Visibility.Visible    : Visibility.Collapsed;
        UsersBtn.Visibility = isAdmin  ? Visibility.Visible    : Visibility.Collapsed;
        AuditBtn.Visibility = isAdmin  ? Visibility.Visible    : Visibility.Collapsed;

        // Sync theme button label on load
        ThemeToggleBtn.Content = ThemeService.IsDark ? "☀ Light" : "🌙 Dark";

        await LoadOrdinancesAsync();
    }

    // ── Theme toggle ───────────────────────────────────────────────────────

    private void ThemeToggleBtn_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.Toggle();

        // Re-render the currently selected ordinance so status badge colors update
        if (_selectedOrdinance is not null)
            ShowDetail(_selectedOrdinance);
    }

    // ── Data loading ───────────────────────────────────────────────────────

    private async Task LoadOrdinancesAsync()
    {
        try
        {
            var query   = _searchQuery;
            var results = await Task.Run(() => _service.Search(query).ToList());

            if (StatusFilter.SelectedItem is ComboBoxItem { Content: string status } && status != "All"
                && Enum.TryParse<OrdinanceStatus>(status.Replace(" ", ""), out var parsed))
                results = results.Where(o => o.Status == parsed).ToList();

            OrdinanceList.ItemsSource = results;
            ResultCount.Text          = $"{results.Count} ordinance(s)";
            ClearDetail();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading ordinances:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Search / filter ────────────────────────────────────────────────────

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        _searchQuery = SearchBox.Text.Trim();
        await LoadOrdinancesAsync();
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) SearchButton_Click(sender, e);
    }

    private async void StatusFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_service is null) return;
        await LoadOrdinancesAsync();
    }

    // ── Ordinance selection ────────────────────────────────────────────────

    private async void OrdinanceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OrdinanceList.SelectedItem is not Ordinance o) return;

        var detail = await Task.Run(() => _service.GetDetails(o.OrdinanceNumber));
        if (detail is null) return;
        _selectedOrdinance = detail;
        ShowDetail(detail);

        _ = Task.Run(() => _auth.LogAction(_currentUser, "VIEW",
            $"Viewed ordinance {o.OrdinanceNumber}"));
    }

    // ── CRUD actions ───────────────────────────────────────────────────────

    private void AddBtn_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AddEditOrdinanceWindow(_service) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            _ = Task.Run(() => _auth.LogAction(_currentUser, "ADD",
                $"Added ordinance {dlg.SavedOrdinance?.OrdinanceNumber}"));
            _ = LoadOrdinancesAsync();
            MessageBox.Show("Ordinance added successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void EditBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedOrdinance is null) return;
        var dlg = new AddEditOrdinanceWindow(_service, _selectedOrdinance)
                  { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            var num = _selectedOrdinance.OrdinanceNumber;
            _ = Task.Run(() => _auth.LogAction(_currentUser, "EDIT", $"Edited ordinance {num}"));
            await LoadOrdinancesAsync();
            var updated = await Task.Run(() => _service.GetDetails(num));
            if (updated is not null) { _selectedOrdinance = updated; ShowDetail(updated); }
        }
    }

    private async void AmendBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedOrdinance is null) return;
        var dlg = new AddAmendmentWindow(_service, _selectedOrdinance)
                  { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            var num = _selectedOrdinance.OrdinanceNumber;
            _ = Task.Run(() => _auth.LogAction(_currentUser, "AMEND", $"Added amendment to {num}"));
            await LoadOrdinancesAsync();
            var updated = await Task.Run(() => _service.GetDetails(num));
            if (updated is not null) { _selectedOrdinance = updated; ShowDetail(updated); }
        }
    }

    private void OpenPdfBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedOrdinance?.DocumentPath is null) return;
        if (!System.IO.File.Exists(_selectedOrdinance.DocumentPath))
        {
            MessageBox.Show("PDF file not found at:\n" + _selectedOrdinance.DocumentPath,
                "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo(_selectedOrdinance.DocumentPath)
                { UseShellExecute = true });
            _ = Task.Run(() => _auth.LogAction(_currentUser, "OPEN_PDF",
                $"Opened PDF for {_selectedOrdinance.OrdinanceNumber}"));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open PDF:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedOrdinance is null) return;
        var result = MessageBox.Show(
            $"Permanently delete {_selectedOrdinance.OrdinanceNumber}?\n\nThis cannot be undone.",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            var num = _selectedOrdinance.OrdinanceNumber;
            await Task.Run(() => _service.Delete(num));
            _ = Task.Run(() => _auth.LogAction(_currentUser, "DELETE",
                $"Deleted ordinance {num}"));
            await LoadOrdinancesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Delete failed:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Navigation ─────────────────────────────────────────────────────────

    private void ReportsBtn_Click(object sender, RoutedEventArgs e)
        => new ReportsWindow(_service) { Owner = Window.GetWindow(this) }.ShowDialog();

    private void UsersBtn_Click(object sender, RoutedEventArgs e)
        => new UserManagementWindow(_auth) { Owner = Window.GetWindow(this) }.ShowDialog();

    private void AuditBtn_Click(object sender, RoutedEventArgs e)
        => new AuditLogWindow(_auth) { Owner = Window.GetWindow(this) }.ShowDialog();

    private async void LogoutBtn_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Are you sure you want to log out?",
            "Logout", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;
        _ = Task.Run(() => _auth.LogAction(_currentUser, "LOGOUT",
            $"{_currentUser.Username} logged out."));
        LogoutRequested?.Invoke();
    }

    // ── ShowDetail ─────────────────────────────────────────────────────────

    private void ShowDetail(Ordinance o)
    {
        DetailPanel.Visibility = Visibility.Visible;
        ActionBar.Visibility   = Visibility.Visible;

        bool canWrite = _currentUser.Role is UserRole.Admin or UserRole.Encoder;
        bool isAdmin  = _currentUser.Role == UserRole.Admin;

        EditBtn.Visibility    = canWrite ? Visibility.Visible    : Visibility.Collapsed;
        AmendBtn.Visibility   = canWrite ? Visibility.Visible    : Visibility.Collapsed;
        DeleteBtn.Visibility  = isAdmin  ? Visibility.Visible    : Visibility.Collapsed;
        OpenPdfBtn.Visibility = o.DocumentPath is not null ? Visibility.Visible : Visibility.Collapsed;

        // ── Hero card ─────────────────────────────────────────────────────
        DetailId.Text      = $"{o.OrdinanceNumber}  ·  {o.SeriesNumber}";
        DetailSubject.Text = o.Subject;
        DetailSeries.Text  = $"{o.Type}  ·  Sponsor: {o.Sponsor}";

        // Status badge - pick colors from theme resources
        (var bgKey, var fgKey) = o.Status switch
        {
            OrdinanceStatus.InEffect    => ("StatusInEffectBgBrush",    "StatusInEffectFgBrush"),
            OrdinanceStatus.Amended     => ("StatusAmendedBgBrush",     "StatusAmendedFgBrush"),
            OrdinanceStatus.Repealed    => ("StatusRepealedBgBrush",    "StatusRepealedFgBrush"),
            OrdinanceStatus.UnderReview => ("StatusReviewBgBrush",      "StatusReviewFgBrush"),
            OrdinanceStatus.Superseded  => ("StatusSupersededBgBrush",  "StatusSupersededFgBrush"),
            _                           => ("StatusReviewBgBrush",      "StatusReviewFgBrush"),
        };
        StatusBadgeControl.SetResourceReference(Border.BackgroundProperty, bgKey);
        DetailStatus.SetResourceReference(TextBlock.ForegroundProperty, fgKey);
        DetailStatus.Text = o.Status switch
        {
            OrdinanceStatus.InEffect    => "In effect",
            OrdinanceStatus.UnderReview => "Under review",
            _                           => o.Status.ToString()
        };

        // Relationship chips
        PdfBadge.Visibility      = !string.IsNullOrEmpty(o.DocumentPath)         ? Visibility.Visible : Visibility.Collapsed;
        AmendsPanel.Visibility   = !string.IsNullOrEmpty(o.AmendsOrdinanceNumber) ? Visibility.Visible : Visibility.Collapsed;
        SupersedesPanel.Visibility = !string.IsNullOrEmpty(o.SupersedesOrdinanceNumber) ? Visibility.Visible : Visibility.Collapsed;
        RepealedByPanel.Visibility = !string.IsNullOrEmpty(o.RepealedByOrdinanceNumber) ? Visibility.Visible : Visibility.Collapsed;

        if (!string.IsNullOrEmpty(o.AmendsOrdinanceNumber))
            AmendsLabel.Text = $"Amends: {o.AmendsOrdinanceNumber}";
        if (!string.IsNullOrEmpty(o.SupersedesOrdinanceNumber))
            SupersedesLabel.Text = $"Supersedes: {o.SupersedesOrdinanceNumber}";
        if (!string.IsNullOrEmpty(o.RepealedByOrdinanceNumber))
            RepealedByLabel.Text = $"Repealed by: {o.RepealedByOrdinanceNumber}";

        // ── Metadata grid (2-column cards) ────────────────────────────────
        MetadataGrid.Children.Clear();
        AddMetaCell(o.Committee,                                     "Committee");
        AddMetaCell(o.DatePassed?.ToString("MMMM dd, yyyy") ?? "—", "Date passed");
        AddMetaCell(o.DateApprovedByMayor?.ToString("MMMM dd, yyyy") ?? "—", "Mayor approved");
        AddMetaCell(o.DatePublished?.ToString("MMMM dd, yyyy") ?? "—", "Date published");
        AddMetaCell($"{o.Versions.Count} version{(o.Versions.Count == 1 ? "" : "s")}", "Versions");
        if (!string.IsNullOrEmpty(o.RepealReason))
            AddMetaCell(o.RepealReason, "Repeal reason", danger: true);

        // ── Latest version card ───────────────────────────────────────────
        LatestVersionPanel.Children.Clear();
        if (o.LatestVersion is OrdinanceVersion latest)
        {
            LatestVersionCard.Visibility = Visibility.Visible;
            VersionNumLabel.Text         = $"Version {latest.VersionNumber} (latest)";
            VersionDateLabel.Text        = $"Enacted {latest.DateEnacted:MMMM dd, yyyy}";

            LatestVersionPanel.Children.Add(MakeVersionRow("Title",      latest.Title,      bold: true));
            LatestVersionPanel.Children.Add(MakeVersionRow("Enacted by", latest.EnactedBy));
            LatestVersionPanel.Children.Add(MakeVersionRow("Content",    latest.Content));

            if (!string.IsNullOrEmpty(latest.AmendmentNotes))
            {
                var notesBorder = new Border
                {
                    Margin        = new Thickness(0, 8, 0, 0),
                    CornerRadius  = new CornerRadius(6),
                    Padding       = new Thickness(10, 8, 10, 8),
                };
                notesBorder.SetResourceReference(Border.BackgroundProperty, "VersionNotesBgBrush");

                var notesText = new TextBlock
                {
                    Text         = latest.AmendmentNotes,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize     = 12,
                    FontStyle    = FontStyles.Italic,
                };
                notesText.SetResourceReference(TextBlock.ForegroundProperty, "VersionNotesFgBrush");
                notesBorder.Child = notesText;
                LatestVersionPanel.Children.Add(notesBorder);
            }
        }
        else
        {
            LatestVersionCard.Visibility = Visibility.Collapsed;
        }

        // ── Version history timeline ──────────────────────────────────────
        var history = o.Versions.OrderBy(v => v.VersionNumber).SkipLast(1).ToList();
        HistoryHeader.Visibility       = history.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        VersionHistoryList.ItemsSource = history;
    }

    private void ClearDetail()
    {
        DetailPanel.Visibility = Visibility.Collapsed;
        ActionBar.Visibility   = Visibility.Collapsed;
        _selectedOrdinance     = null;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>Adds a themed metadata cell to the UniformGrid.</summary>
    private void AddMetaCell(string value, string label, bool danger = false)
    {
        var cell = new Border
        {
            Margin        = new Thickness(0, 0, 8, 8),
            CornerRadius  = new CornerRadius(8),
            Padding       = new Thickness(12, 10, 12, 10),
        };
        cell.SetResourceReference(Border.BackgroundProperty, "BgSecondaryBrush");

        var inner = new StackPanel();

        var keyBlock = new TextBlock
        {
            Text       = label.ToUpper(),
            FontSize   = 10,
            FontWeight = FontWeights.SemiBold,
            Margin     = new Thickness(0, 0, 0, 3),
        };
        keyBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextTertiaryBrush");
        inner.Children.Add(keyBlock);

        var valBlock = new TextBlock
        {
            Text         = value,
            FontSize     = 12,
            FontWeight   = FontWeights.Medium,
            TextWrapping = TextWrapping.Wrap,
        };
        if (danger)
            valBlock.SetResourceReference(TextBlock.ForegroundProperty, "DangerFgBrush");
        else
            valBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");

        inner.Children.Add(valBlock);
        cell.Child = inner;
        MetadataGrid.Children.Add(cell);
    }

    /// <summary>Creates a label+value row for the latest version card body.</summary>
    private StackPanel MakeVersionRow(string label, string value, bool bold = false)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin      = new Thickness(0, 0, 0, 6),
        };

        var lbl = new TextBlock
        {
            Text       = label + ": ",
            FontSize   = 12,
            FontWeight = FontWeights.SemiBold,
            MinWidth   = 100,
        };
        lbl.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");

        var val = new TextBlock
        {
            Text         = value,
            FontSize     = 12,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth     = 460,
            FontWeight   = bold ? FontWeights.SemiBold : FontWeights.Normal,
        };
        val.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");

        panel.Children.Add(lbl);
        panel.Children.Add(val);
        return panel;
    }
}
