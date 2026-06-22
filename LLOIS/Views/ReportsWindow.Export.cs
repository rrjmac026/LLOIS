namespace LLOIS.Views;

using System.Windows;
using LLOIS.Models;

public partial class ReportsWindow
{
    private void ExportPdfBtn_Click(object sender, RoutedEventArgs e)
    {
        var path = PickSavePath("PDF Files (*.pdf)|*.pdf", ".pdf", $"LLOIS_{CurrentTabTitle}");
        if (path is null) return;

        try
        {
            using var doc = new PdfSharp.Pdf.PdfDocument();
            var page      = doc.AddPage();
            page.Orientation = PdfSharp.PageOrientation.Landscape;
            PdfSharp.Drawing.XGraphics gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);

            var boldFont   = new PdfSharp.Drawing.XFont("Arial", 9,  PdfSharp.Drawing.XFontStyleEx.Bold);
            var normalFont = new PdfSharp.Drawing.XFont("Arial", 8,  PdfSharp.Drawing.XFontStyleEx.Regular);
            var titleFont  = new PdfSharp.Drawing.XFont("Arial", 13, PdfSharp.Drawing.XFontStyleEx.Bold);
            var subFont    = new PdfSharp.Drawing.XFont("Arial", 9,  PdfSharp.Drawing.XFontStyleEx.Regular);

            var headerBrush = new PdfSharp.Drawing.XSolidBrush(PdfSharp.Drawing.XColor.FromArgb(26, 58, 107));
            var altBrush    = new PdfSharp.Drawing.XSolidBrush(PdfSharp.Drawing.XColor.FromArgb(248, 249, 255));
            var grayBrush   = new PdfSharp.Drawing.XSolidBrush(PdfSharp.Drawing.XColor.FromArgb(136, 136, 136));
            var whiteBrush  = PdfSharp.Drawing.XBrushes.White;
            var blackBrush  = PdfSharp.Drawing.XBrushes.Black;

            double margin  = 30;
            double y       = margin;
            double pgWidth = page.Width.Point - margin * 2;

            gfx.DrawString("Local Legislative Ordinance Information System",
                titleFont, blackBrush,
                new PdfSharp.Drawing.XRect(margin, y, pgWidth, 20),
                PdfSharp.Drawing.XStringFormats.TopLeft);
            y += 18;
            gfx.DrawString($"Report: {CurrentTabTitle}  |  Generated: {DateTime.Now:MMMM dd, yyyy}",
                subFont, grayBrush,
                new PdfSharp.Drawing.XRect(margin, y, pgWidth, 14),
                PdfSharp.Drawing.XStringFormats.TopLeft);
            y += 20;

            double[] colWidths = [ pgWidth*.11, pgWidth*.12, pgWidth*.28,
                                   pgWidth*.09, pgWidth*.09, pgWidth*.16, pgWidth*.10 ];
            double rowH = 16;

            void DrawRow(string[] cells, bool isHeader, bool isAlt)
            {
                double x = margin;
                if (isHeader)
                    gfx.DrawRectangle(headerBrush, new PdfSharp.Drawing.XRect(margin, y, pgWidth, rowH));
                else if (isAlt)
                    gfx.DrawRectangle(altBrush,    new PdfSharp.Drawing.XRect(margin, y, pgWidth, rowH));

                var font  = isHeader ? boldFont   : normalFont;
                var brush = isHeader ? whiteBrush : blackBrush;

                for (int i = 0; i < cells.Length && i < colWidths.Length; i++)
                {
                    gfx.DrawString(cells[i], font, brush,
                        new PdfSharp.Drawing.XRect(x + 2, y + 2, colWidths[i] - 4, rowH - 2),
                        PdfSharp.Drawing.XStringFormats.TopLeft);
                    x += colWidths[i];
                }
                y += rowH;
            }

            DrawRow(Columns.Select(c => c.Header).ToArray(), isHeader: true, isAlt: false);

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
            gfx.DrawString($"Total: {_currentData.Count} ordinance(s)", boldFont, blackBrush,
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
}