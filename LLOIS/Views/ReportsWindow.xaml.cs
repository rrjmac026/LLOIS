namespace LLOIS.Views;

using System.Text;
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
        // Populate year filter
        YearCombo.Items.Clear();
        YearCombo.Items.Add(new ComboBoxItem { Content = "All Years" });
        foreach (var year in _service.GetAvailableYears())
            YearCombo.Items.Add(new ComboBoxItem { Content = year.ToString() });
        YearCombo.SelectedIndex = 0;

        LoadReport();
    }

    private void ReportTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_service is null) return;       // ← add this
        if (!IsLoaded) return;              // ← add this (if not already there)

        var tab = (ReportTabs.SelectedItem as TabItem)?.Header?.ToString();
        YearFilterPanel.Visibility   = tab == "By Year"   ? Visibility.Visible : Visibility.Collapsed;
        StatusFilterPanel.Visibility = tab == "By Status" ? Visibility.Visible : Visibility.Collapsed;

        LoadReport();
    }

    private void Filter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_service is null) return;   // ← add this
        LoadReport();
    }

    private void LoadReport()
    {
        if (_service is null) return;                    // ← add this
        if (ReportTabs.SelectedItem is null) return;     // ← add this

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

    private void PrintBtn_Click(object sender, RoutedEventArgs e)
    {
        var tab = (ReportTabs.SelectedItem as TabItem)?.Header?.ToString() ?? "Report";
        var doc = BuildPrintDocument(tab);

        var dlg = new PrintDialog();
        if (dlg.ShowDialog() == true)
            dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator,
                $"LLOIS Report — {tab}");
    }

    private FlowDocument BuildPrintDocument(string title)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 11,
            PagePadding = new Thickness(60, 50, 60, 50)
        };

        // Header
        doc.Blocks.Add(new Paragraph(new Run("🏛️ Local Legislative Ordinance Information System"))
        {
            FontSize = 16, FontWeight = FontWeights.Bold
        });
        doc.Blocks.Add(new Paragraph(new Run($"Report: {title}  |  Generated: {DateTime.Now:MMMM dd, yyyy}"))
        {
            FontSize = 11, Foreground = Brushes.Gray
        });

        // Table
        var table = new Table();
        table.Columns.Add(new TableColumn { Width = new GridLength(110) });
        table.Columns.Add(new TableColumn { Width = new GridLength(130) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(80) });
        table.Columns.Add(new TableColumn { Width = new GridLength(80) });
        table.Columns.Add(new TableColumn { Width = new GridLength(90) });

        var headerGroup = new TableRowGroup();
        var headerRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(26, 58, 107)) };
        foreach (var h in new[] { "Ord. No.", "Series", "Title", "Type", "Status", "Date Passed" })
        {
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run(h))
            {
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontSize = 10
            })
            { Padding = new Thickness(4, 3, 4, 3) });
        }
        headerGroup.Rows.Add(headerRow);
        table.RowGroups.Add(headerGroup);

        var bodyGroup = new TableRowGroup();
        bool alt = false;
        foreach (var o in _currentData)
        {
            var row = new TableRow
            {
                Background = alt ? new SolidColorBrush(Color.FromRgb(248, 249, 255)) : Brushes.White
            };
            alt = !alt;
            foreach (var val in new[]
            {
                o.OrdinanceNumber, o.SeriesNumber, o.Title,
                o.Type.ToString(), o.Status.ToString(),
                o.DatePassed?.ToString("MM/dd/yyyy") ?? "—"
            })
            {
                row.Cells.Add(new TableCell(new Paragraph(new Run(val)) { FontSize = 10 })
                { Padding = new Thickness(4, 2, 4, 2) });
            }
            bodyGroup.Rows.Add(row);
        }
        table.RowGroups.Add(bodyGroup);
        doc.Blocks.Add(table);

        doc.Blocks.Add(new Paragraph(new Run($"Total: {_currentData.Count} ordinance(s)"))
        {
            FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 0)
        });

        return doc;
    }
}
