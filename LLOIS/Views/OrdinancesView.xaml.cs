namespace LLOIS.Views;

using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LLOIS.Models;
using LLOIS.Services;

public partial class OrdinancesView : UserControl
{
    private readonly IOrdinanceService _service;
    private readonly IAuthService      _auth;
    private readonly User              _currentUser;
    private string     _searchQuery      = string.Empty;
    private Ordinance? _selectedOrdinance;
    private bool       _loaded           = false;

    public OrdinancesView(IOrdinanceService service, IAuthService auth, User user)
    {
        InitializeComponent();
        _service     = service;
        _auth        = auth;
        _currentUser = user;

        bool canWrite = user.Role is UserRole.Admin or UserRole.Encoder;
        bool isAdmin  = user.Role == UserRole.Admin;

        AddBtn.Visibility = canWrite ? Visibility.Visible : Visibility.Collapsed;
    }

    public void ReloadIfNeeded()
    {
        if (!_loaded)
        {
            _loaded = true;
            _ = LoadOrdinancesAsync();
        }
    }

    public void ApplySearch(string query)
    {
        _searchQuery  = query;
        SearchBox.Text = query;
        _ = LoadOrdinancesAsync();
    }

    // ── Data loading ───────────────────────────────────────────────────────

    private async Task LoadOrdinancesAsync()
    {
        try
        {
            var query   = _searchQuery;
            var results = await Task.Run(() => _service.Search(query).ToList());

            // Status filter
            if (StatusFilter.SelectedItem is ComboBoxItem { Content: string status }
                && status != "All statuses")
            {
                var parsed = status.Replace(" ", "") switch
                {
                    "InEffect"    => OrdinanceStatus.InEffect,
                    "Amended"     => OrdinanceStatus.Amended,
                    "Superseded"  => OrdinanceStatus.Superseded,
                    "Repealed"    => OrdinanceStatus.Repealed,
                    "UnderReview" => OrdinanceStatus.UnderReview,
                    _             => (OrdinanceStatus?)null
                };
                if (parsed.HasValue)
                    results = results.Where(o => o.Status == parsed.Value).ToList();
            }

            // Type filter
            if (TypeFilter.SelectedItem is ComboBoxItem { Content: string typeName }
                && typeName != "All types"
                && Enum.TryParse<OrdinanceType>(typeName, out var parsedType))
            {
                results = results.Where(o => o.Type == parsedType).ToList();
            }

            OrdinanceList.ItemsSource = results;
            ResultCount.Text = $"{results.Count} ordinances found";
            ClearDetail();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading ordinances:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Search / filter ────────────────────────────────────────────────────

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        _searchQuery = SearchBox.Text.Trim();
        await LoadOrdinancesAsync();
    }

    private async void StatusFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_service is null) return;
        await LoadOrdinancesAsync();
    }

    private async void TypeFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_service is null) return;
        await LoadOrdinancesAsync();
    }

    // ── Selection ──────────────────────────────────────────────────────────

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

    // ── CRUD ───────────────────────────────────────────────────────────────

    private void AddBtn_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AddEditOrdinanceWindow(_service) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            _ = Task.Run(() => _auth.LogAction(_currentUser, "ADD",
                $"Added ordinance {dlg.SavedOrdinance?.OrdinanceNumber}"));
            _loaded = false;
            ReloadIfNeeded();
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
            _ = Task.Run(() => _auth.LogAction(_currentUser, "EDIT", $"Edited {num}"));
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
            _ = Task.Run(() => _auth.LogAction(_currentUser, "AMEND", $"Amended {num}"));
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

    // ── ShowDetail ─────────────────────────────────────────────────────────

    private void ShowDetail(Ordinance o)
    {
        DetailPanel.Visibility = Visibility.Visible;
        ActionBar.Visibility   = Visibility.Visible;

        bool canWrite = _currentUser.Role is UserRole.Admin or UserRole.Encoder;
        bool isAdmin  = _currentUser.Role == UserRole.Admin;

        // Topbar action buttons (hidden; we use inline row below hero card now)
        EditBtn.Visibility    = Visibility.Collapsed;
        AmendBtn.Visibility   = Visibility.Collapsed;
        DeleteBtn.Visibility  = isAdmin  ? Visibility.Visible : Visibility.Collapsed;
        OpenPdfBtn.Visibility = Visibility.Collapsed;
        ActionBar.Visibility  = isAdmin  ? Visibility.Visible : Visibility.Collapsed;

        // Inline action row under hero card
        InlineActionRow.Visibility   = canWrite ? Visibility.Visible : Visibility.Collapsed;
        InlineEditBtn.Visibility     = canWrite ? Visibility.Visible : Visibility.Collapsed;
        InlineAmendBtn.Visibility    = canWrite ? Visibility.Visible : Visibility.Collapsed;
        InlineOpenPdfBtn.Visibility  = o.DocumentPath is not null ? Visibility.Visible : Visibility.Collapsed;

        // Hero
        DetailId.Text      = $"{o.OrdinanceNumber}  ·  {o.SeriesNumber}";
        DetailSubject.Text = o.Subject;
        DetailSeries.Text  = $"{o.Type}  ·  Sponsor: {o.Sponsor}";
        VersionCountLabel.Text = $"  {o.Versions.Count} version{(o.Versions.Count == 1 ? "" : "s")}";

        // Status badge
        (var bgKey, var fgKey) = o.Status switch
        {
            OrdinanceStatus.InEffect    => ("StatusInEffectBgBrush", "StatusInEffectFgBrush"),
            OrdinanceStatus.Amended     => ("StatusAmendedBgBrush",  "StatusAmendedFgBrush"),
            OrdinanceStatus.Repealed    => ("StatusRepealedBgBrush", "StatusRepealedFgBrush"),
            OrdinanceStatus.UnderReview => ("StatusReviewBgBrush",   "StatusReviewFgBrush"),
            OrdinanceStatus.Superseded  => ("StatusSupersededBgBrush","StatusSupersededFgBrush"),
            _                           => ("StatusReviewBgBrush",   "StatusReviewFgBrush"),
        };
        StatusBadgeControl.SetResourceReference(Border.BackgroundProperty, bgKey);
        DetailStatus.SetResourceReference(TextBlock.ForegroundProperty, fgKey);
        DetailStatus.Text = o.Status switch
        {
            OrdinanceStatus.InEffect    => "In effect",
            OrdinanceStatus.UnderReview => "Under review",
            _                           => o.Status.ToString()
        };

        // Chips
        PdfBadge.Visibility       = !string.IsNullOrEmpty(o.DocumentPath) ? Visibility.Visible : Visibility.Collapsed;
        AmendsPanel.Visibility    = !string.IsNullOrEmpty(o.AmendsOrdinanceNumber) ? Visibility.Visible : Visibility.Collapsed;
        RepealedByPanel.Visibility = !string.IsNullOrEmpty(o.RepealedByOrdinanceNumber) ? Visibility.Visible : Visibility.Collapsed;
        if (!string.IsNullOrEmpty(o.AmendsOrdinanceNumber))
            AmendsLabel.Text = $"Amends: {o.AmendsOrdinanceNumber}";
        if (!string.IsNullOrEmpty(o.RepealedByOrdinanceNumber))
            RepealedByLabel.Text = $"Repealed by: {o.RepealedByOrdinanceNumber}";

        // Metadata grid
        MetadataGrid.Children.Clear();
        AddMetaCell(o.Committee, "Committee");
        AddMetaCell(o.DatePassed?.ToString("MMMM dd, yyyy") ?? "—", "Date passed");
        AddMetaCell(o.DateApprovedByMayor?.ToString("MMMM dd, yyyy") ?? "—", "Mayor approved");
        AddMetaCell(o.DatePublished?.ToString("MMMM dd, yyyy") ?? "—", "Date published");
        if (!string.IsNullOrEmpty(o.RepealReason))
            AddMetaCell(o.RepealReason, "Repeal reason", danger: true);

        // Latest version
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
                var nb = new Border { Margin = new Thickness(0,8,0,0), CornerRadius = new CornerRadius(6), Padding = new Thickness(10,8,10,8) };
                nb.SetResourceReference(Border.BackgroundProperty, "VersionNotesBgBrush");
                var nt = new TextBlock { Text = latest.AmendmentNotes, TextWrapping = TextWrapping.Wrap, FontSize = 12, FontStyle = FontStyles.Italic };
                nt.SetResourceReference(TextBlock.ForegroundProperty, "VersionNotesFgBrush");
                nb.Child = nt;
                LatestVersionPanel.Children.Add(nb);
            }
        }
        else
        {
            LatestVersionCard.Visibility = Visibility.Collapsed;
        }

        // History
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

    private void AddMetaCell(string value, string label, bool danger = false)
    {
        var cell = new Border { Margin = new Thickness(0,0,8,8), CornerRadius = new CornerRadius(8), Padding = new Thickness(12,10,12,10) };
        cell.SetResourceReference(Border.BackgroundProperty, "BgSecondaryBrush");
        var inner = new StackPanel();
        var key = new TextBlock { Text = label.ToUpper(), FontSize = 10, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0,0,0,3) };
        key.SetResourceReference(TextBlock.ForegroundProperty, "TextTertiaryBrush");
        inner.Children.Add(key);
        var val = new TextBlock { Text = value, FontSize = 12, FontWeight = FontWeights.Medium, TextWrapping = TextWrapping.Wrap };
        if (danger) val.SetResourceReference(TextBlock.ForegroundProperty, "DangerFgBrush");
        else        val.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        inner.Children.Add(val);
        cell.Child = inner;
        MetadataGrid.Children.Add(cell);
    }

    private StackPanel MakeVersionRow(string label, string value, bool bold = false)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,0,6) };
        var lbl = new TextBlock { Text = label + ": ", FontSize = 12, FontWeight = FontWeights.SemiBold, MinWidth = 100 };
        lbl.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
        var val = new TextBlock { Text = value, FontSize = 12, TextWrapping = TextWrapping.Wrap, MaxWidth = 460, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal };
        val.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        panel.Children.Add(lbl);
        panel.Children.Add(val);
        return panel;
    }
}
