namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using LLOIS.Models;
using LLOIS.Services;

public partial class AddEditOrdinanceWindow : Window
{
    private readonly IOrdinanceService _service;
    private readonly Ordinance? _existing;
    private readonly bool _isEdit;

    public Ordinance? SavedOrdinance { get; private set; }

    // Add mode
    public AddEditOrdinanceWindow(IOrdinanceService service)
    {
        InitializeComponent();
        _service = service;
        _isEdit = false;
        WindowTitle.Text = "➕ Add New Ordinance";
        StatusCombo.SelectedIndex = 0;
        TypeCombo.SelectedIndex = 0;
    }

    // Edit mode
    public AddEditOrdinanceWindow(IOrdinanceService service, Ordinance existing)
    {
        InitializeComponent();
        _service = service;
        _existing = existing;
        _isEdit = true;
        WindowTitle.Text = $"✏️ Edit Ordinance — {existing.OrdinanceNumber}";

        VersionSectionHeader.Visibility = Visibility.Collapsed;
        VersionSeparator.Visibility     = Visibility.Collapsed;
        VersionSection.Visibility       = Visibility.Collapsed;

        PopulateFields(existing);
    }

    private void PopulateFields(Ordinance o)
    {
        OrdNumberBox.Text    = o.OrdinanceNumber;
        OrdNumberBox.IsReadOnly = true;
        SeriesBox.Text       = o.SeriesNumber;
        TitleBox.Text        = o.Title;
        SubjectBox.Text      = o.Subject;
        SponsorBox.Text      = o.Sponsor;
        CommitteeBox.Text    = o.Committee;
        AmendsBox.Text       = o.AmendsOrdinanceNumber ?? "";
        SupersedesBox.Text   = o.SupersedesOrdinanceNumber ?? "";
        RepealedByBox.Text   = o.RepealedByOrdinanceNumber ?? "";
        RepealReasonBox.Text = o.RepealReason ?? "";
        PdfPathBox.Text      = o.DocumentPath ?? "";

        SetComboByContent(TypeCombo, o.Type.ToString());
        SetComboByContent(StatusCombo, o.Status switch
        {
            OrdinanceStatus.InEffect    => "In Effect",
            OrdinanceStatus.UnderReview => "Under Review",
            _ => o.Status.ToString()
        });

        if (o.DatePassed.HasValue)
            DatePassedPicker.SelectedDate = o.DatePassed.Value.ToDateTime(TimeOnly.MinValue);
        if (o.DateApprovedByMayor.HasValue)
            DateApprovedPicker.SelectedDate = o.DateApprovedByMayor.Value.ToDateTime(TimeOnly.MinValue);
        if (o.DatePublished.HasValue)
            DatePublishedPicker.SelectedDate = o.DatePublished.Value.ToDateTime(TimeOnly.MinValue);
    }

    private static void SetComboByContent(ComboBox combo, string content)
    {
        foreach (ComboBoxItem item in combo.Items)
        {
            if (item.Content?.ToString() == content)
            { combo.SelectedItem = item; return; }
        }
        combo.SelectedIndex = 0;
    }

    private void BrowsePdf_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Ordinance PDF",
            Filter = "PDF Files (*.pdf)|*.pdf",
            CheckFileExists = true
        };
        if (dlg.ShowDialog() == true)
            PdfPathBox.Text = dlg.FileName;
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}