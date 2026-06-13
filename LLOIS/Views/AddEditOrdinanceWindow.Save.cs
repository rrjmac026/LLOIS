namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using LLOIS.Models;

public partial class AddEditOrdinanceWindow
{
    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!Validate()) return;
        try
        {
            if (_isEdit) SaveEdit();
            else         SaveNew();
            DialogResult = true;
        }
        catch (Exception ex) { ShowError($"Save failed: {ex.Message}"); }
    }

    private void SaveNew()
    {
        if (VersionDatePicker.SelectedDate is null)
        { ShowError("Please select a Date Enacted for the initial version."); return; }

        var ordinance = BuildOrdinance();
        ordinance.Versions.Add(new OrdinanceVersion
        {
            VersionNumber = 1,
            Title        = VersionTitleBox.Text.Trim(),
            Content      = VersionContentBox.Text.Trim(),
            EnactedBy    = EnactedByBox.Text.Trim(),
            DateEnacted  = DateOnly.FromDateTime(VersionDatePicker.SelectedDate.Value)
        });

        _service.Add(ordinance);
        SavedOrdinance = ordinance;
    }

    private void SaveEdit()
    {
        var o = _existing!;
        o.SeriesNumber              = SeriesBox.Text.Trim();
        o.Title                     = TitleBox.Text.Trim();
        o.Subject                   = SubjectBox.Text.Trim();
        o.Sponsor                   = SponsorBox.Text.Trim();
        o.Committee                 = CommitteeBox.Text.Trim();
        o.Type                      = ParseType();
        o.Status                    = ParseStatus();
        o.DatePassed                = DatePassedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DatePassedPicker.SelectedDate.Value) : null;
        o.DateApprovedByMayor       = DateApprovedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DateApprovedPicker.SelectedDate.Value) : null;
        o.DatePublished             = DatePublishedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DatePublishedPicker.SelectedDate.Value) : null;
        o.AmendsOrdinanceNumber     = NullIfEmpty(AmendsBox.Text);
        o.SupersedesOrdinanceNumber = NullIfEmpty(SupersedesBox.Text);
        o.RepealedByOrdinanceNumber = NullIfEmpty(RepealedByBox.Text);
        o.RepealReason              = NullIfEmpty(RepealReasonBox.Text);
        o.DocumentPath              = NullIfEmpty(PdfPathBox.Text);

        _service.Update(o);
        SavedOrdinance = o;
    }

    private Ordinance BuildOrdinance() => new()
    {
        OrdinanceNumber             = OrdNumberBox.Text.Trim(),
        SeriesNumber                = SeriesBox.Text.Trim(),
        Title                       = TitleBox.Text.Trim(),
        Subject                     = SubjectBox.Text.Trim(),
        Sponsor                     = SponsorBox.Text.Trim(),
        Committee                   = CommitteeBox.Text.Trim(),
        Type                        = ParseType(),
        Status                      = ParseStatus(),
        DatePassed                  = DatePassedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DatePassedPicker.SelectedDate.Value) : null,
        DateApprovedByMayor         = DateApprovedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DateApprovedPicker.SelectedDate.Value) : null,
        DatePublished               = DatePublishedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DatePublishedPicker.SelectedDate.Value) : null,
        AmendsOrdinanceNumber       = NullIfEmpty(AmendsBox.Text),
        SupersedesOrdinanceNumber   = NullIfEmpty(SupersedesBox.Text),
        RepealedByOrdinanceNumber   = NullIfEmpty(RepealedByBox.Text),
        RepealReason                = NullIfEmpty(RepealReasonBox.Text),
        DocumentPath                = NullIfEmpty(PdfPathBox.Text)
    };

    private OrdinanceType ParseType() =>
        (TypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
        {
            "Revenue"        => OrdinanceType.Revenue,
            "Administrative" => OrdinanceType.Administrative,
            "Penal"          => OrdinanceType.Penal,
            "Appropriation"  => OrdinanceType.Appropriation,
            "Other"          => OrdinanceType.Other,
            _                => OrdinanceType.Regulatory
        };

    private OrdinanceStatus ParseStatus() =>
        (StatusCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
        {
            "Amended"      => OrdinanceStatus.Amended,
            "Superseded"   => OrdinanceStatus.Superseded,
            "Repealed"     => OrdinanceStatus.Repealed,
            "Under Review" => OrdinanceStatus.UnderReview,
            _              => OrdinanceStatus.InEffect
        };

    private bool Validate()
    {
        HideError();
        if (string.IsNullOrWhiteSpace(OrdNumberBox.Text))  return ShowError("Ordinance Number is required.");
        if (string.IsNullOrWhiteSpace(SeriesBox.Text))     return ShowError("Series Number is required.");
        if (string.IsNullOrWhiteSpace(TitleBox.Text))      return ShowError("Title is required.");
        if (string.IsNullOrWhiteSpace(SubjectBox.Text))    return ShowError("Subject is required.");
        if (string.IsNullOrWhiteSpace(SponsorBox.Text))    return ShowError("Sponsor is required.");
        if (!_isEdit)
        {
            if (string.IsNullOrWhiteSpace(VersionTitleBox.Text))   return ShowError("Version Title is required.");
            if (string.IsNullOrWhiteSpace(VersionContentBox.Text)) return ShowError("Version Content is required.");
            if (VersionDatePicker.SelectedDate is null)            return ShowError("Date Enacted (initial version) is required.");
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
}