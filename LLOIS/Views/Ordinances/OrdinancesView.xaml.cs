namespace LLOIS.Views;

using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LLOIS.Services;

public partial class OrdinancesView : UserControl
{
    private readonly IOrdinanceService _service;
    private readonly ApiUser           _currentUser;
    private string        _searchQuery      = string.Empty;
    private ApiOrdinance? _selectedOrdinance;
    private bool          _loaded           = false;
    private bool          _suppressSelection = false;

    private readonly System.Windows.Threading.DispatcherTimer _searchTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(300)
    };

    private readonly System.Windows.Threading.DispatcherTimer _refreshTimer = new()
    {
        Interval = TimeSpan.FromSeconds(15)
    };

    public OrdinancesView(IOrdinanceService service, ApiUser user)
    {
        InitializeComponent();
        _service     = service;
        _currentUser = user;

        bool canWrite = user.RoleName is "Admin" or "SuperAdmin" or "Encoder";
        AddBtn.Visibility = canWrite ? Visibility.Visible : Visibility.Collapsed;

        _searchTimer.Tick += SearchTimer_Tick;
        _refreshTimer.Tick += async (_, _) =>
        {
            if (IsVisible && SessionContext.CurrentUser is not null)
                await LoadOrdinancesAsync(preserveSelection: true);
        };
        _refreshTimer.Start();
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

    private async Task LoadOrdinancesAsync(bool preserveSelection = false)
    {
        var selectedId = preserveSelection ? _selectedOrdinance?.Id : null;
        try
        {
            var query = _searchQuery;

            string? statusFilter = null;
            if (StatusFilter.SelectedItem is ComboBoxItem { Content: string status }
                && status != "All statuses")
            {
                statusFilter = status.Replace(" ", "").ToLowerInvariant() switch
                {
                    "ineffect"    => "in_effect",
                    "amended"     => "amended",
                    "superseded"  => "superseded",
                    "repealed"    => "repealed",
                    "underreview" => "under_review",
                    _             => null
                };
            }

            var results = (await _service.SearchAsync(query, statusFilter)).ToList();

            // Type filter (client-side — no server param for this one)
            if (TypeFilter.SelectedItem is ComboBoxItem { Content: string typeName }
                && typeName != "All types")
            {
                results = results.Where(o =>
                    string.Equals(o.Type, typeName, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            _suppressSelection = true;
            try
            {
                OrdinanceList.ItemsSource = results;
                ResultCount.Text = $"{results.Count} ordinances found";

                var selected = selectedId.HasValue
                    ? results.FirstOrDefault(o => o.Id == selectedId.Value)
                    : null;
                if (selected is null)
                {
                    ClearDetail();
                    return;
                }

                OrdinanceList.SelectedItem = selected;
                var detail = await _service.GetDetailsAsync(selected.Id);
                if (detail is null)
                {
                    ClearDetail();
                    return;
                }

                _selectedOrdinance = detail;
                ShowDetail(detail);
            }
            finally
            {
                _suppressSelection = false;
            }
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.HandleIfApiFailure(ex))
                return;

            MessageBox.Show($"Error loading ordinances:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Search / filter ────────────────────────────────────────────────────

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        _searchTimer.Stop();
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
        if (_suppressSelection) return;
        if (OrdinanceList.SelectedItem is not ApiOrdinance o) return;

        try
        {
            var detail = await _service.GetDetailsAsync(o.Id);
            if (detail is null) return;

            _selectedOrdinance = detail;
            ShowDetail(detail);
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.HandleIfApiFailure(ex))
                return;

            MessageBox.Show($"Error loading ordinance details:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── CRUD ───────────────────────────────────────────────────────────────

    private void AddBtn_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AddEditOrdinanceWindow(_service) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            _loaded = false;
            ReloadIfNeeded();

            MessageBox.Show("Ordinance created successfully.", "Success",
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
            var id = _selectedOrdinance.Id;
            await LoadOrdinancesAsync();
            var updated = await _service.GetDetailsAsync(id);
            if (updated is not null) { _selectedOrdinance = updated; ShowDetail(updated); }

            MessageBox.Show("Ordinance updated successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OpenPdfBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedOrdinance?.DocumentPath is null) return;

        try
        {
            Process.Start(new ProcessStartInfo(_selectedOrdinance.DocumentPath)
                { UseShellExecute = true });
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
            var id = _selectedOrdinance.Id;
            await _service.DeleteAsync(id);
            await LoadOrdinancesAsync();

            MessageBox.Show("Ordinance deleted successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.HandleIfApiFailure(ex))
                return;

            MessageBox.Show($"Delete failed:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── ShowDetail ─────────────────────────────────────────────────────────

    private void ShowDetail(ApiOrdinance o)
    {
        DetailPanel.Visibility = Visibility.Visible;
        ActionBar.Visibility   = Visibility.Visible;

        bool canWrite = _currentUser.RoleName is "Admin" or "Encoder";

        EditBtn.Visibility    = Visibility.Collapsed;
        DeleteBtn.Visibility  = canWrite  ? Visibility.Visible : Visibility.Collapsed;
        OpenPdfBtn.Visibility = Visibility.Collapsed;
        ActionBar.Visibility  = canWrite  ? Visibility.Visible : Visibility.Collapsed;

        InlineActionRow.Visibility   = canWrite ? Visibility.Visible : Visibility.Collapsed;
        InlineEditBtn.Visibility     = canWrite ? Visibility.Visible : Visibility.Collapsed;
        InlineOpenPdfBtn.Visibility  = o.DocumentPath is not null ? Visibility.Visible : Visibility.Collapsed;

        DetailId.Text      = $"{o.OrdinanceNumber}  ·  {o.SeriesNumber}";
        DetailSubject.Text = o.Subject;
        DetailSeries.Text  = $"{o.Type}  ·  Sponsor: {o.Sponsor}";
        VersionCountLabel.Text = $"  {o.Versions.Count} version{(o.Versions.Count == 1 ? "" : "s")}";

        (var bgKey, var fgKey) = o.Status.ToLowerInvariant() switch
        {
            "in_effect"    => ("StatusInEffectBgBrush", "StatusInEffectFgBrush"),
            "amended"      => ("StatusAmendedBgBrush",  "StatusAmendedFgBrush"),
            "repealed"     => ("StatusRepealedBgBrush", "StatusRepealedFgBrush"),
            "under_review" => ("StatusReviewBgBrush",   "StatusReviewFgBrush"),
            "superseded"   => ("StatusSupersededBgBrush","StatusSupersededFgBrush"),
            _              => ("StatusReviewBgBrush",   "StatusReviewFgBrush"),
        };
        StatusBadgeControl.SetResourceReference(Border.BackgroundProperty, bgKey);
        DetailStatus.SetResourceReference(TextBlock.ForegroundProperty, fgKey);
        DetailStatus.Text = o.Status.ToLowerInvariant() switch
        {
            "in_effect"    => "In effect",
            "under_review" => "Under review",
            _              => o.Status
        };

        PdfBadge.Visibility = !string.IsNullOrEmpty(o.DocumentPath) ? Visibility.Visible : Visibility.Collapsed;

        MetadataGrid.Children.Clear();
        AddMetaCell(o.Committee ?? "—", "Committee");
        AddMetaCell(o.DatePassed?.ToString("MMMM dd, yyyy") ?? "—", "Date passed");
        AddMetaCell(o.DateApproved?.ToString("MMMM dd, yyyy") ?? "—", "Date approved");
        AddMetaCell(o.DatePublished?.ToString("MMMM dd, yyyy") ?? "—", "Date published");
        if (!string.IsNullOrEmpty(o.Location))
            AddMetaCell(o.Location, "Location");
        if (!string.IsNullOrEmpty(o.ReferenceNumber))
            AddMetaCell(o.ReferenceNumber, "Reference number");
        if (!string.IsNullOrEmpty(o.FinalAction))
            AddMetaCell(o.FinalAction, "Final action");
        if (!string.IsNullOrEmpty(o.State))
            AddMetaCell(o.State, "State");
        if (!string.IsNullOrEmpty(o.AddedBy))
            AddMetaCell(o.AddedBy, "Added by");

        LatestVersionPanel.Children.Clear();
        if (o.LatestVersion is ApiOrdinanceVersion latest)
        {
            LatestVersionCard.Visibility = Visibility.Visible;
            VersionNumLabel.Text         = $"Version {latest.VersionNumber} (latest)";
            LatestVersionPanel.Children.Add(MakeVersionRow("Title",   latest.Title, bold: true));
            LatestVersionPanel.Children.Add(MakeVersionRow("Content", latest.Content));
        }
        else
        {
            LatestVersionCard.Visibility = Visibility.Collapsed;
        }

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

    private async void SearchTimer_Tick(object? sender, EventArgs e)
    {
        _searchTimer.Stop();
        _searchQuery = SearchBox.Text.Trim();
        await LoadOrdinancesAsync();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchTimer.Stop();
        _searchTimer.Start();
    }
}