namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using LLOIS.Services;

public partial class ReportsView : UserControl
{
    private readonly IReportService _service;
    private List<ApiOrdinance> _currentData = [];
    private bool _initialized;
    private int  _loadId;   // guards against out-of-order responses when tabs/filters change quickly

    public ReportsView(IReportService service)
    {
        InitializeComponent();
        _service = service;
    }

    /// <summary>
    /// Called by MainView whenever the Reports page is shown. Only does the
    /// one-time year-combo setup the first time; reloads the active report
    /// every time so it stays fresh if ordinances changed elsewhere.
    /// </summary>
    public void ReloadIfNeeded() => _ = ReloadIfNeededAsync();

    private async Task ReloadIfNeededAsync()
    {
        if (!_initialized)
        {
            try
            {
                var years = await _service.GetOrdinanceYearsAsync();

                // _initialized is still false here, so the SelectionChanged events fired
                // by filling the combo are ignored (no duplicate loads).
                YearCombo.Items.Clear();
                YearCombo.Items.Add(new ComboBoxItem { Content = "All Years" });
                foreach (var year in years)
                    YearCombo.Items.Add(new ComboBoxItem { Content = year.ToString() });
                YearCombo.SelectedIndex = 0;

                _initialized = true;
            }
            catch (Exception ex)
            {
                if (ConnectionFailureHandler.HandleIfApiFailure(ex))
                    return;

                MessageBox.Show($"Error loading reports data:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        await LoadReportAsync();
    }

    private async void ReportTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // ComboBox SelectionChanged bubbles up to the TabControl — ignore those.
        if (e.OriginalSource != ReportTabs) return;
        if (_service is null || !_initialized) return;

        var tab = (ReportTabs.SelectedItem as TabItem)?.Header?.ToString();
        YearFilterPanel.Visibility   = tab == "By Year"   ? Visibility.Visible : Visibility.Collapsed;
        StatusFilterPanel.Visibility = tab == "By Status" ? Visibility.Visible : Visibility.Collapsed;
        await LoadReportAsync();
    }

    private async void Filter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_service is null || !_initialized) return;
        await LoadReportAsync();
    }

    private async Task LoadReportAsync()
    {
        if (_service is null || ReportTabs.SelectedItem is null) return;

        var loadId = ++_loadId;
        try
        {
            var tab = (ReportTabs.SelectedItem as TabItem)?.Header?.ToString() ?? "All Ordinances";
            var data = tab switch
            {
                "By Year"   => await LoadByYearAsync(),
                "By Status" => await LoadByStatusAsync(),
                "Repealed"  => await _service.GetOrdinancesAsync(status: "repealed"),
                "Amended"   => await _service.GetOrdinancesAsync(amendedOnly: true),
                _           => await _service.GetOrdinancesAsync()
            };

            if (loadId != _loadId) return;   // a newer request superseded this one

            _currentData = data;
            ReportGrid.ItemsSource = _currentData;
            RecordCount.Text = $"{_currentData.Count} record(s)";
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.HandleIfApiFailure(ex))
                return;

            MessageBox.Show($"Error loading reports:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private Task<List<ApiOrdinance>> LoadByYearAsync()
    {
        int? year = YearCombo.SelectedItem is ComboBoxItem { Content: string s } && int.TryParse(s, out var y)
            ? y : null;
        return _service.GetOrdinancesAsync(year: year);
    }

    private Task<List<ApiOrdinance>> LoadByStatusAsync()
    {
        var statusText = (StatusCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "In Effect";
        var status = statusText switch
        {
            "Amended"      => "amended",
            "Superseded"   => "superseded",
            "Under Review" => "under_review",
            _              => "in_effect"
        };
        return _service.GetOrdinancesAsync(status: status);
    }

    // ── Shared helpers ───────────────────────────────────────────────────────

    private static readonly (string Header, Func<ApiOrdinance, string> Value)[] Columns =
    [
        ("Ord. Number", o => o.OrdinanceNumber),
        ("Series",      o => o.SeriesNumber ?? ""),
        ("Title",       o => o.Title),
        ("Type",        o => Pretty(o.Type)),
        ("Status",      o => Pretty(o.Status)),
        ("Sponsor",     o => o.Sponsor ?? ""),
        ("Date Passed", o => o.DatePassed?.ToString("MM/dd/yyyy") ?? "—"),
    ];

    // "in_effect" -> "In Effect" (Laravel sends raw enum values)
    private static string Pretty(string? value) =>
        string.IsNullOrEmpty(value)
            ? ""
            : string.Join(' ', value.Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(w => char.ToUpper(w[0]) + w[1..]));

    private string CurrentTabTitle =>
        (ReportTabs.SelectedItem as TabItem)?.Header?.ToString() ?? "Report";

    private static string? PickSavePath(string filter, string ext, string defaultName)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
            { Filter = filter, FileName = defaultName, DefaultExt = ext };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    private static void OpenFile(string path)
    {
        try { System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true }); }
        catch { }
    }

    private static string HtmlEncode(string? s) =>
        (s ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    // Exports/prints are local, so tell the server to record them (fire-and-forget, never blocks the user).
    private void LogReportAction(string action, string details) =>
        _ = Task.Run(async () =>
        {
            try { await _service.LogReportActionAsync(action, details); }
            catch { /* non-critical */ }
        });
}