namespace LLOIS.Views;

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using LLOIS.Models;

public partial class ReportsView
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
