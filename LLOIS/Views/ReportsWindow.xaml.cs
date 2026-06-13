namespace LLOIS.Views;

using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using LLOIS.Models;
using LLOIS.Services;
using System.IO;

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

    // ── Shared helpers ───────────────────────────────────────────────────────────

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
        {
            Filter      = filter,
            FileName    = defaultName,
            DefaultExt  = ext
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    private static void OpenFile(string path)
    {
        try
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch { /* ignore if OS can't open it */ }
    }

    // ── Export PDF ──────────────────────────────────────────────────────────────

    private void ExportPdfBtn_Click(object sender, RoutedEventArgs e)
    {
        var path = PickSavePath("PDF Files (*.pdf)|*.pdf", ".pdf", $"LLOIS_{CurrentTabTitle}");
        if (path is null) return;

        try
        {
            using var doc      = new PdfSharp.Pdf.PdfDocument();
            var page           = doc.AddPage();
            page.Orientation   = PdfSharp.PageOrientation.Landscape;
            PdfSharp.Drawing.XGraphics gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);

            var boldFont   = new PdfSharp.Drawing.XFont("Arial", 9, PdfSharp.Drawing.XFontStyleEx.Bold);
            var normalFont = new PdfSharp.Drawing.XFont("Arial", 8, PdfSharp.Drawing.XFontStyleEx.Regular);
            var titleFont  = new PdfSharp.Drawing.XFont("Arial", 13, PdfSharp.Drawing.XFontStyleEx.Bold);
            var subFont    = new PdfSharp.Drawing.XFont("Arial", 9, PdfSharp.Drawing.XFontStyleEx.Regular);

            var headerBrush  = new PdfSharp.Drawing.XSolidBrush(
                PdfSharp.Drawing.XColor.FromArgb(26, 58, 107));
            var altBrush     = new PdfSharp.Drawing.XSolidBrush(
                PdfSharp.Drawing.XColor.FromArgb(248, 249, 255));
            var whiteBrush   = PdfSharp.Drawing.XBrushes.White;
            var blackBrush   = PdfSharp.Drawing.XBrushes.Black;
            var grayBrush    = new PdfSharp.Drawing.XSolidBrush(
                PdfSharp.Drawing.XColor.FromArgb(136, 136, 136));

            double margin  = 30;
            double y       = margin;
            double pgWidth = page.Width.Point - margin * 2;

            // Title
            gfx.DrawString("Local Legislative Ordinance Information System",
                titleFont, blackBrush, new PdfSharp.Drawing.XRect(margin, y, pgWidth, 20),
                PdfSharp.Drawing.XStringFormats.TopLeft);
            y += 18;
            gfx.DrawString(
                $"Report: {CurrentTabTitle}  |  Generated: {DateTime.Now:MMMM dd, yyyy}",
                subFont, grayBrush, new PdfSharp.Drawing.XRect(margin, y, pgWidth, 14),
                PdfSharp.Drawing.XStringFormats.TopLeft);
            y += 20;

            // Column widths (proportional)
            double[] colWidths = [ pgWidth * .11, pgWidth * .12, pgWidth * .30,
                                pgWidth * .09, pgWidth * .09, pgWidth * .16, pgWidth * .10 ];
            double rowH = 16;

            // Helper: draw one row
            void DrawRow(string[] cells, bool isHeader, bool isAlt)
            {
                double x = margin;
                // Background
                if (isHeader)
                    gfx.DrawRectangle(headerBrush,
                        new PdfSharp.Drawing.XRect(margin, y, pgWidth, rowH));
                else if (isAlt)
                    gfx.DrawRectangle(altBrush,
                        new PdfSharp.Drawing.XRect(margin, y, pgWidth, rowH));

                var font  = isHeader ? boldFont : normalFont;
                var brush = isHeader ? whiteBrush : blackBrush;

                for (int i = 0; i < cells.Length && i < colWidths.Length; i++)
                {
                    var rect = new PdfSharp.Drawing.XRect(x + 2, y + 2, colWidths[i] - 4, rowH - 2);
                    // Clip long text
                    var text = cells[i];
                    gfx.DrawString(text, font, brush, rect, PdfSharp.Drawing.XStringFormats.TopLeft);
                    x += colWidths[i];
                }
                y += rowH;
            }

            // Header row
            DrawRow(Columns.Select(c => c.Header).ToArray(), isHeader: true, isAlt: false);

            // Data rows — add new pages as needed
            bool alt = false;
            foreach (var o in _currentData)
            {
                if (y + rowH > page.Height.Point - margin)
                {
                    page = doc.AddPage();
                    page.Orientation = PdfSharp.PageOrientation.Landscape;
                    gfx.Dispose();
                    gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);
                    y = margin;
                    DrawRow(Columns.Select(c => c.Header).ToArray(), isHeader: true, isAlt: false);
                }

                DrawRow(Columns.Select(c => c.Value(o)).ToArray(), isHeader: false, isAlt: alt);
                alt = !alt;
            }

            y += 6;
            gfx.DrawString($"Total: {_currentData.Count} ordinance(s)",
                boldFont, blackBrush,
                new PdfSharp.Drawing.XRect(margin, y, pgWidth, 14),
                PdfSharp.Drawing.XStringFormats.TopLeft);

            doc.Save(path);

            MessageBox.Show($"PDF exported to:\n{path}", "Export Successful",
                MessageBoxButton.OK, MessageBoxImage.Information);
            OpenFile(path);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"PDF export failed:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Export Excel ─────────────────────────────────────────────────────────────

    private void ExportExcelBtn_Click(object sender, RoutedEventArgs e)
    {
        var path = PickSavePath("Excel Files (*.xlsx)|*.xlsx", ".xlsx", $"LLOIS_{CurrentTabTitle}");
        if (path is null) return;

        try
        {
            using var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.Worksheets.Add(
                CurrentTabTitle.Length > 31 ? CurrentTabTitle[..31] : CurrentTabTitle);

            // Header row
            for (int c = 0; c < Columns.Length; c++)
            {
                var cell = ws.Cell(1, c + 1);
                cell.Value = Columns[c].Header;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(26, 58, 107);
                cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                cell.Style.Alignment.WrapText = true;
            }

            // Data rows
            for (int r = 0; r < _currentData.Count; r++)
            {
                var o = _currentData[r];
                for (int c = 0; c < Columns.Length; c++)
                    ws.Cell(r + 2, c + 1).Value = Columns[c].Value(o);

                if (r % 2 == 1)
                    ws.Row(r + 2).Style.Fill.BackgroundColor =
                        ClosedXML.Excel.XLColor.FromArgb(248, 249, 255);
            }

            // Footer
            int footerRow = _currentData.Count + 3;
            var footer = ws.Cell(footerRow, 1);
            footer.Value = $"Total: {_currentData.Count} ordinance(s)";
            footer.Style.Font.Bold = true;

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);

            wb.SaveAs(path);

            MessageBox.Show($"Excel exported to:\n{path}", "Export Successful",
                MessageBoxButton.OK, MessageBoxImage.Information);
            OpenFile(path);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Excel export failed:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Export Word (HTML→.doc) ───────────────────────────────────────────────────

    private static string HtmlEncode(string? s) =>
    (s ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private void ExportWordBtn_Click(object sender, RoutedEventArgs e)
    {
        var path = PickSavePath("Word Document (*.doc)|*.doc", ".doc", $"LLOIS_{CurrentTabTitle}");
        if (path is null) return;

        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("""
                <html xmlns:o='urn:schemas-microsoft-com:office:office'
                    xmlns:w='urn:schemas-microsoft-com:office:word'
                    xmlns='http://www.w3.org/TR/REC-html40'>
                <head>
                <meta charset='utf-8'>
                <style>
                    body { font-family: Calibri, sans-serif; font-size: 10pt; }
                    h1   { font-size: 14pt; color: #1a3a6b; margin-bottom: 2px; }
                    p.sub{ font-size: 9pt; color: #888; margin-top: 0; }
                    table{ border-collapse: collapse; width: 100%; }
                    th   { background:#1a3a6b; color:#fff; padding:5px 8px;
                        font-size:9pt; text-align:left; }
                    td   { padding:4px 8px; font-size:9pt; border-bottom:1px solid #ddd; }
                    tr.alt td { background:#f8f9ff; }
                    p.total   { font-weight:bold; margin-top:10px; }
                </style>
                </head>
                <body>
                """);

            sb.AppendLine($"<h1>Local Legislative Ordinance Information System</h1>");
            sb.AppendLine($"<p class='sub'>Report: {CurrentTabTitle} &nbsp;|&nbsp; " +
                        $"Generated: {DateTime.Now:MMMM dd, yyyy}</p>");
            sb.AppendLine("<table>");

            // Header
            sb.Append("<tr>");
            foreach (var col in Columns)
                sb.Append($"<th>{HtmlEncode(col.Header)}</th>");
            sb.AppendLine("</tr>");

            // Data rows
            bool alt = false;
            foreach (var o in _currentData)
            {
                sb.Append(alt ? "<tr class='alt'>" : "<tr>");
                foreach (var col in Columns)
                    sb.Append($"<td>{HtmlEncode(col.Value(o))}</td>");
                sb.AppendLine("</tr>");
                alt = !alt;
            }

            sb.AppendLine("</table>");
            sb.AppendLine($"<p class='total'>Total: {_currentData.Count} ordinance(s)</p>");
            sb.AppendLine("</body></html>");

            File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);

            MessageBox.Show($"Word document exported to:\n{path}", "Export Successful",
                MessageBoxButton.OK, MessageBoxImage.Information);
            OpenFile(path);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Word export failed:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
