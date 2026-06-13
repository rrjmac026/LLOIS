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
        StatusCombo.SelectedIndex = 0; // In Effect
        TypeCombo.SelectedIndex = 0;   // Regulatory
    }

    // Edit mode
    public AddEditOrdinanceWindow(IOrdinanceService service, Ordinance existing)
    {
        InitializeComponent();
        _service = service;
        _existing = existing;
        _isEdit = true;
        WindowTitle.Text = $"✏️ Edit Ordinance — {existing.OrdinanceNumber}";

        // Hide the initial version section (already has versions)
        VersionSectionHeader.Visibility = Visibility.Collapsed;
        VersionSeparator.Visibility = Visibility.Collapsed;
        VersionSection.Visibility = Visibility.Collapsed;

        PopulateFields(existing);
    }

    private void PopulateFields(Ordinance o)
    {
        OrdNumberBox.Text = o.OrdinanceNumber;
        OrdNumberBox.IsReadOnly = true; // can't change primary key
        SeriesBox.Text = o.SeriesNumber;
        TitleBox.Text = o.Title;
        SubjectBox.Text = o.Subject;
        SponsorBox.Text = o.Sponsor;
        CommitteeBox.Text = o.Committee;
        AmendsBox.Text = o.AmendsOrdinanceNumber ?? "";
        SupersedesBox.Text = o.SupersedesOrdinanceNumber ?? "";
        RepealedByBox.Text = o.RepealedByOrdinanceNumber ?? "";
        RepealReasonBox.Text = o.RepealReason ?? "";
        PdfPathBox.Text = o.DocumentPath ?? "";

        SetComboByContent(TypeCombo, o.Type.ToString());
        SetComboByContent(StatusCombo, o.Status switch
        {
            OrdinanceStatus.InEffect => "In Effect",
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
            {
                combo.SelectedItem = item;
                return;
            }
        }
        combo.SelectedIndex = 0;
    }

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!Validate()) return;

        try
        {
            if (_isEdit)
                SaveEdit();
            else
                SaveNew();

            DialogResult = true;
        }
        catch (Exception ex)
        {
            ShowError($"Save failed: {ex.Message}");
        }
    }

    private void SaveNew()
    {
        if (VersionDatePicker.SelectedDate is null)
        {
            ShowError("Please select a Date Enacted for the initial version.");
            return;
        }

        var ordinance = BuildOrdinance();
        ordinance.Versions.Add(new OrdinanceVersion
        {
            VersionNumber = 1,
            Title = VersionTitleBox.Text.Trim(),
            Content = VersionContentBox.Text.Trim(),
            EnactedBy = EnactedByBox.Text.Trim(),
            DateEnacted = DateOnly.FromDateTime(VersionDatePicker.SelectedDate.Value)
        });

        _service.Add(ordinance);
        SavedOrdinance = ordinance;
    }

    private void SaveEdit()
    {
        var o = _existing!;
        o.SeriesNumber = SeriesBox.Text.Trim();
        o.Title = TitleBox.Text.Trim();
        o.Subject = SubjectBox.Text.Trim();
        o.Sponsor = SponsorBox.Text.Trim();
        o.Committee = CommitteeBox.Text.Trim();
        o.Type = ParseType();
        o.Status = ParseStatus();
        o.DatePassed = DatePassedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DatePassedPicker.SelectedDate.Value) : null;
        o.DateApprovedByMayor = DateApprovedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DateApprovedPicker.SelectedDate.Value) : null;
        o.DatePublished = DatePublishedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DatePublishedPicker.SelectedDate.Value) : null;
        o.AmendsOrdinanceNumber = NullIfEmpty(AmendsBox.Text);
        o.SupersedesOrdinanceNumber = NullIfEmpty(SupersedesBox.Text);
        o.RepealedByOrdinanceNumber = NullIfEmpty(RepealedByBox.Text);
        o.RepealReason = NullIfEmpty(RepealReasonBox.Text);
        o.DocumentPath = NullIfEmpty(PdfPathBox.Text);

        _service.Update(o);
        SavedOrdinance = o;
    }

    private Ordinance BuildOrdinance() => new()
    {
        OrdinanceNumber = OrdNumberBox.Text.Trim(),
        SeriesNumber = SeriesBox.Text.Trim(),
        Title = TitleBox.Text.Trim(),
        Subject = SubjectBox.Text.Trim(),
        Sponsor = SponsorBox.Text.Trim(),
        Committee = CommitteeBox.Text.Trim(),
        Type = ParseType(),
        Status = ParseStatus(),
        DatePassed = DatePassedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DatePassedPicker.SelectedDate.Value) : null,
        DateApprovedByMayor = DateApprovedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DateApprovedPicker.SelectedDate.Value) : null,
        DatePublished = DatePublishedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DatePublishedPicker.SelectedDate.Value) : null,
        AmendsOrdinanceNumber = NullIfEmpty(AmendsBox.Text),
        SupersedesOrdinanceNumber = NullIfEmpty(SupersedesBox.Text),
        RepealedByOrdinanceNumber = NullIfEmpty(RepealedByBox.Text),
        RepealReason = NullIfEmpty(RepealReasonBox.Text),
        DocumentPath = NullIfEmpty(PdfPathBox.Text)
    };

    private OrdinanceType ParseType() =>
        (TypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
        {
            "Revenue" => OrdinanceType.Revenue,
            "Administrative" => OrdinanceType.Administrative,
            "Penal" => OrdinanceType.Penal,
            "Appropriation" => OrdinanceType.Appropriation,
            "Other" => OrdinanceType.Other,
            _ => OrdinanceType.Regulatory
        };

    private OrdinanceStatus ParseStatus() =>
        (StatusCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
        {
            "Amended" => OrdinanceStatus.Amended,
            "Superseded" => OrdinanceStatus.Superseded,
            "Repealed" => OrdinanceStatus.Repealed,
            "Under Review" => OrdinanceStatus.UnderReview,
            _ => OrdinanceStatus.InEffect
        };

    private bool Validate()
    {
        HideError();
        if (string.IsNullOrWhiteSpace(OrdNumberBox.Text))
            return ShowError("Ordinance Number is required.");
        if (string.IsNullOrWhiteSpace(SeriesBox.Text))
            return ShowError("Series Number is required.");
        if (string.IsNullOrWhiteSpace(TitleBox.Text))
            return ShowError("Title is required.");
        if (string.IsNullOrWhiteSpace(SubjectBox.Text))
            return ShowError("Subject is required.");
        if (string.IsNullOrWhiteSpace(SponsorBox.Text))
            return ShowError("Sponsor is required.");
        if (!_isEdit)
        {
            if (string.IsNullOrWhiteSpace(VersionTitleBox.Text))
                return ShowError("Version Title is required.");
            if (string.IsNullOrWhiteSpace(VersionContentBox.Text))
                return ShowError("Version Content is required.");
            if (VersionDatePicker.SelectedDate is null)
                return ShowError("Date Enacted (initial version) is required.");
        }
        return true;
    }

    private bool ShowError(string msg)
    {
        ErrorText.Text = msg;
        ErrorBanner.Visibility = Visibility.Visible;
        return false;
    }

    private void HideError() => ErrorBanner.Visibility = Visibility.Collapsed;

    private void CancelBtn_Click(object sender, RoutedEventArgs e) => DialogResult = false;

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

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
