namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using LLOIS.Models;
using LLOIS.Services;

public partial class ReportsWindow : Window
{
    private readonly IOrdinanceService _service;
    private List<Ordinance> _currentData = [];

    public ReportsWindow(IOrdinanceService service)
    {
        InitializeComponent();
        _service = service;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        YearCombo.Items.Clear();
        YearCombo.Items.Add(new ComboBoxItem { Content = "All Years" });
        foreach (var year in _service.GetAvailableYears())
            YearCombo.Items.Add(new ComboBoxItem { Content = year.ToString() });
        YearCombo.SelectedIndex = 0;
        LoadReport();
    }

    private void ReportTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_service is null || !IsLoaded) return;
        var tab = (ReportTabs.SelectedItem as TabItem)?.Header?.ToString();
        YearFilterPanel.Visibility   = tab == "By Year"   ? Visibility.Visible : Visibility.Collapsed;
        StatusFilterPanel.Visibility = tab == "By Status" ? Visibility.Visible : Visibility.Collapsed;
        LoadReport();
    }

    private void Filter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_service is null) return;
        LoadReport();
    }

    private void LoadReport()
    {
        if (_service is null || ReportTabs.SelectedItem is null) return;
        var tab = (ReportTabs.SelectedItem as TabItem)?.Header?.ToString() ?? "All Ordinances";
        _currentData = tab switch
        {
            "By Year"   => LoadByYear(),
            "By Status" => LoadByStatus(),
            "Repealed"  => _service.GetByStatus(OrdinanceStatus.Repealed).ToList(),
            "Amended"   => _service.Search("").Where(o => o.HasAmendments).ToList(),
            _           => _service.Search("").ToList()
        };
        ReportGrid.ItemsSource = _currentData;
        RecordCount.Text = $"{_currentData.Count} record(s)";
    }

    private List<Ordinance> LoadByYear()
    {
        var all = _service.Search("").ToList();
        if (YearCombo.SelectedItem is ComboBoxItem { Content: string s } && int.TryParse(s, out var year))
            return all.Where(o => o.DatePassed?.Year == year).ToList();
        return all;
    }

    private List<Ordinance> LoadByStatus()
    {
        var statusText = (StatusCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "In Effect";
        var status = statusText switch
        {
            "Amended"      => OrdinanceStatus.Amended,
            "Superseded"   => OrdinanceStatus.Superseded,
            "Under Review" => OrdinanceStatus.UnderReview,
            _              => OrdinanceStatus.InEffect
        };
        return _service.GetByStatus(status).ToList();
    }

    // ── Shared helpers ───────────────────────────────────────────────────────

    private static readonly (string Header, Func<Ordinance, string> Value)[] Columns =
    [
        ("Ord. Number", o => o.OrdinanceNumber),
        ("Series",      o => o.SeriesNumber),
        ("Title",       o => o.Title),
        ("Type",        o => o.Type.ToString()),
        ("Status",      o => o.Status.ToString()),
        ("Sponsor",     o => o.Sponsor),
        ("Date Passed", o => o.DatePassed?.ToString("MM/dd/yyyy") ?? "—"),
    ];

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
}