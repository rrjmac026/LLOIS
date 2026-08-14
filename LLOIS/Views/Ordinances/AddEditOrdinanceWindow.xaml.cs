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

        // Always show the version section in Edit mode now — either to edit
        // the existing latest version, or to create a missing Version 1.
        VersionSectionCard.Visibility   = Visibility.Visible;
        VersionSectionHeader.Visibility = Visibility.Visible;
        VersionSeparator.Visibility     = Visibility.Visible;
        VersionSection.Visibility       = Visibility.Visible;

        PopulateFields(existing);

        var latest = existing.LatestVersion;
        if (latest is not null)
        {
            VersionTitleBox.Text   = latest.Title;
            VersionContentBox.Text = latest.Content;
            EnactedByBox.Text      = latest.EnactedBy;
            VersionDatePicker.SelectedDate = latest.DateEnacted.ToDateTime(TimeOnly.MinValue);
        }
    }

    private void PopulateFields(Ordinance o)
    {
        OrdNumberBox.Text       = o.OrdinanceNumber;
        OrdNumberBox.IsReadOnly = true;
        SeriesBox.Text          = o.SeriesNumber;
        TitleBox.Text           = o.Title;
        SubjectBox.Text         = o.Subject;
        SponsorBox.Text         = o.Sponsor;
        CommitteeBox.Text       = o.Committee;
        PdfPathBox.Text         = o.DocumentPath ?? "";
        ReferenceNumberBox.Text = o.ReferenceNumber ?? "";
        NrsNsbBox.Text          = o.NRS_NSB ?? "";
        NomenclatureBox.Text    = o.Nomenclature ?? "";
        LocationBox.Text        = o.Location ?? "";

        SetComboByContent(TypeCombo, o.Type.ToString());
        SetComboByContent(StatusCombo, o.Status switch
        {
            OrdinanceStatus.InEffect    => "In Effect",
            OrdinanceStatus.UnderReview => "Under Review",
            _ => o.Status.ToString()
        });
        if (o.FinalAction.HasValue)
            SetComboByContent(FinalActionCombo, o.FinalAction.Value.ToString());
        if (o.State.HasValue)
            SetComboByContent(StateCombo, o.State.Value.ToString());

        if (o.DatePassed.HasValue)
            DatePassedPicker.SelectedDate = o.DatePassed.Value.ToDateTime(TimeOnly.MinValue);
        if (o.DateApproved.HasValue)
            DateApprovedPicker.SelectedDate = o.DateApproved.Value.ToDateTime(TimeOnly.MinValue);
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
        if (dlg.ShowDialog() != true) return;

        try
        {
            PdfPathBox.Text = "Uploading...";
            var url = StorageService.UploadPdf(dlg.FileName);
            PdfPathBox.Text = url;
        }
        catch (Exception ex)
        {
            PdfPathBox.Text = "";
            if (!ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                MessageBox.Show($"Upload failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!Validate()) return;
        try
        {
            if (_isEdit) SaveEdit();
            else         SaveNew();
            DialogResult = true;
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                return;

            ShowError($"Save failed: {ex.Message}");
        }
    }

    private void SaveNew()
    {
        if (VersionDatePicker.SelectedDate is null)
        { ShowError("Please select a Date Enacted for the initial version."); return; }

        var ordinance = BuildOrdinance();
        ordinance.AddedBy = SessionContext.CurrentUser?.Username;
        ordinance.AddedAt = DateTime.UtcNow;

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
        o.SeriesNumber    = SeriesBox.Text.Trim();
        o.Title           = TitleBox.Text.Trim();
        o.Subject         = SubjectBox.Text.Trim();
        o.Sponsor         = SponsorBox.Text.Trim();
        o.Committee       = CommitteeBox.Text.Trim();
        o.Type            = ParseType();
        o.Status          = ParseStatus();
        o.FinalAction     = ParseFinalAction();
        o.State           = ParseState();
        o.ReferenceNumber = NullIfEmpty(ReferenceNumberBox.Text);
        o.NRS_NSB         = NullIfEmpty(NrsNsbBox.Text);
        o.Nomenclature    = NullIfEmpty(NomenclatureBox.Text);
        o.Location        = NullIfEmpty(LocationBox.Text);
        o.DatePassed      = DatePassedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DatePassedPicker.SelectedDate.Value) : null;
        o.DateApproved    = DateApprovedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DateApprovedPicker.SelectedDate.Value) : null;
        o.DatePublished   = DatePublishedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DatePublishedPicker.SelectedDate.Value) : null;
        o.DocumentPath    = NullIfEmpty(PdfPathBox.Text);

        var latest = o.LatestVersion;
        if (latest is not null && VersionDatePicker.SelectedDate is not null)
        {
            // Edit the existing latest version directly — no new version created.
            latest.Title       = VersionTitleBox.Text.Trim();
            latest.Content     = VersionContentBox.Text.Trim();
            latest.EnactedBy   = EnactedByBox.Text.Trim();
            latest.DateEnacted = DateOnly.FromDateTime(VersionDatePicker.SelectedDate.Value);
        }
        else if (o.Versions.Count == 0 && VersionDatePicker.SelectedDate is not null)
        {
            // No version exists yet — create Version 1.
            o.Versions.Add(new OrdinanceVersion
            {
                VersionNumber = 1,
                Title        = VersionTitleBox.Text.Trim(),
                Content      = VersionContentBox.Text.Trim(),
                EnactedBy    = EnactedByBox.Text.Trim(),
                DateEnacted  = DateOnly.FromDateTime(VersionDatePicker.SelectedDate.Value)
            });
        }

        _service.Update(o);
        SavedOrdinance = o;
    }

    private Ordinance BuildOrdinance() => new()
    {
        OrdinanceNumber   = OrdNumberBox.Text.Trim(),
        SeriesNumber      = SeriesBox.Text.Trim(),
        Title             = TitleBox.Text.Trim(),
        Subject           = SubjectBox.Text.Trim(),
        Sponsor           = SponsorBox.Text.Trim(),
        Committee         = CommitteeBox.Text.Trim(),
        Type              = ParseType(),
        Status            = ParseStatus(),
        FinalAction       = ParseFinalAction(),
        State             = ParseState(),
        ReferenceNumber   = NullIfEmpty(ReferenceNumberBox.Text),
        NRS_NSB           = NullIfEmpty(NrsNsbBox.Text),
        Nomenclature      = NullIfEmpty(NomenclatureBox.Text),
        Location          = NullIfEmpty(LocationBox.Text),
        DatePassed        = DatePassedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DatePassedPicker.SelectedDate.Value) : null,
        DateApproved      = DateApprovedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DateApprovedPicker.SelectedDate.Value) : null,
        DatePublished     = DatePublishedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DatePublishedPicker.SelectedDate.Value) : null,
        DocumentPath      = NullIfEmpty(PdfPathBox.Text)
    };

    private TypeOfLaw ParseType() =>
        (TypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
        {
            "Resolution" => TypeOfLaw.Resolution,
            "Minutes"    => TypeOfLaw.Minutes,
            _            => TypeOfLaw.Ordinance
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

    private FinalAction? ParseFinalAction() =>
        (FinalActionCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
        {
            "Approving"   => Models.FinalAction.Approving,
            "Authorizing" => Models.FinalAction.Authorizing,
            "Creating"    => Models.FinalAction.Creating,
            "Declaring"   => Models.FinalAction.Declaring,
            "Conducting"  => Models.FinalAction.Conducting,
            "Extending"   => Models.FinalAction.Extending,
            _             => null
        };

    private OrdinanceState? ParseState() =>
        (StateCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
        {
            "Draft"    => OrdinanceState.Draft,
            "Passed"   => OrdinanceState.Passed,
            "Enacted"  => OrdinanceState.Enacted,
            _          => null
        };

    private bool Validate()
    {
        HideError();
        if (string.IsNullOrWhiteSpace(OrdNumberBox.Text))  return ShowError("Ordinance Number is required.");
        if (string.IsNullOrWhiteSpace(SeriesBox.Text))     return ShowError("Series Number is required.");
        if (string.IsNullOrWhiteSpace(TitleBox.Text))      return ShowError("Title is required.");
        if (string.IsNullOrWhiteSpace(SubjectBox.Text))    return ShowError("Subject is required.");
        if (string.IsNullOrWhiteSpace(SponsorBox.Text))    return ShowError("Sponsor is required.");

        if (string.IsNullOrWhiteSpace(VersionTitleBox.Text))   return ShowError("Version Title is required.");
        if (string.IsNullOrWhiteSpace(VersionContentBox.Text)) return ShowError("Version Content is required.");
        if (VersionDatePicker.SelectedDate is null)            return ShowError("Date Enacted is required.");

        return true;
    }

    private bool ShowError(string msg)
    {
        ErrorText.Text = msg;
        ErrorBanner.Visibility = Visibility.Visible;
        return false;
    }

    private void RemovePdfBtn_Click(object sender, RoutedEventArgs e)
    {
        PdfPathBox.Text = "";
    }

    private void HideError() => ErrorBanner.Visibility = Visibility.Collapsed;
}