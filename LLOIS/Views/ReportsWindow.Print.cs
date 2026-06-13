namespace LLOIS.Views;

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using LLOIS.Models;

public partial class ReportsWindow
{
    private void PrintBtn_Click(object sender, RoutedEventArgs e)
    {
        var tab = CurrentTabTitle;
        var doc = BuildFlowDocument(tab);
        var dlg = new PrintDialog();
        if (dlg.ShowDialog() == true)
            dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator,
                $"LLOIS Report — {tab}");
    }

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
                <head><meta charset='utf-8'>
                <style>
                  body{font-family:Calibri,sans-serif;font-size:10pt;}
                  h1{font-size:14pt;color:#1a3a6b;margin-bottom:2px;}
                  p.sub{font-size:9pt;color:#888;margin-top:0;}
                  table{border-collapse:collapse;width:100%;}
                  th{background:#1a3a6b;color:#fff;padding:5px 8px;font-size:9pt;text-align:left;}
                  td{padding:4px 8px;font-size:9pt;border-bottom:1px solid #ddd;}
                  tr.alt td{background:#f8f9ff;}
                  p.total{font-weight:bold;margin-top:10px;}
                </style></head><body>
                """);

            sb.AppendLine($"<h1>Local Legislative Ordinance Information System</h1>");
            sb.AppendLine($"<p class='sub'>Report: {CurrentTabTitle} &nbsp;|&nbsp; Generated: {DateTime.Now:MMMM dd, yyyy}</p>");
            sb.AppendLine("<table><tr>");
            foreach (var col in Columns) sb.Append($"<th>{HtmlEncode(col.Header)}</th>");
            sb.AppendLine("</tr>");

            bool alt = false;
            foreach (var o in _currentData)
            {
                sb.Append(alt ? "<tr class='alt'>" : "<tr>");
                foreach (var col in Columns) sb.Append($"<td>{HtmlEncode(col.Value(o))}</td>");
                sb.AppendLine("</tr>");
                alt = !alt;
            }

            sb.AppendLine($"</table><p class='total'>Total: {_currentData.Count} ordinance(s)</p></body></html>");
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

    private FlowDocument BuildFlowDocument(string title)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 11,
            PagePadding = new Thickness(60, 50, 60, 50)
        };

        doc.Blocks.Add(new Paragraph(new Run("Local Legislative Ordinance Information System"))
            { FontSize = 16, FontWeight = FontWeights.Bold });
        doc.Blocks.Add(new Paragraph(new Run($"Report: {title}  |  Generated: {DateTime.Now:MMMM dd, yyyy}"))
            { FontSize = 11, Foreground = Brushes.Gray });

        var table = new Table();
        table.Columns.Add(new TableColumn { Width = new GridLength(110) });
        table.Columns.Add(new TableColumn { Width = new GridLength(130) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(80) });
        table.Columns.Add(new TableColumn { Width = new GridLength(80) });
        table.Columns.Add(new TableColumn { Width = new GridLength(160) });
        table.Columns.Add(new TableColumn { Width = new GridLength(90) });

        var hGroup = new TableRowGroup();
        var hRow   = new TableRow { Background = new SolidColorBrush(Color.FromRgb(26, 58, 107)) };
        foreach (var col in Columns)
            hRow.Cells.Add(new TableCell(
                new Paragraph(new Run(col.Header))
                    { FontWeight = FontWeights.Bold, Foreground = Brushes.White, FontSize = 10 })
                { Padding = new Thickness(4, 3, 4, 3) });
        hGroup.Rows.Add(hRow);
        table.RowGroups.Add(hGroup);

        var bGroup = new TableRowGroup();
        bool alt = false;
        foreach (var o in _currentData)
        {
            var row = new TableRow
            {
                Background = alt
                    ? new SolidColorBrush(Color.FromRgb(248, 249, 255))
                    : Brushes.White
            };
            alt = !alt;
            foreach (var col in Columns)
                row.Cells.Add(new TableCell(
                    new Paragraph(new Run(col.Value(o))) { FontSize = 10 })
                    { Padding = new Thickness(4, 2, 4, 2) });
            bGroup.Rows.Add(row);
        }
        table.RowGroups.Add(bGroup);
        doc.Blocks.Add(table);

        doc.Blocks.Add(new Paragraph(new Run($"Total: {_currentData.Count} ordinance(s)"))
            { FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 0) });

        return doc;
    }
}