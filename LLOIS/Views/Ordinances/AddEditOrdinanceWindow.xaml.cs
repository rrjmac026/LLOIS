namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using LLOIS.Services;

public partial class AddEditOrdinanceWindow : Window
{
    private readonly IOrdinanceService _service;
    private readonly ApiOrdinance? _existing;
    private readonly bool _isEdit;
    private string? _pickedLocalPdfPath;

    public ApiOrdinance? SavedOrdinance { get; private set; }

    // Add mode
    public AddEditOrdinanceWindow(IOrdinanceService service)
    {
        InitializeComponent();
        _service = service;
        _isEdit = false;
        WindowTitle.Text = "➕ Add New Ordinance";
        StatusCombo.SelectedIndex = 0;
        TypeCombo.SelectedIndex = 0;

        VersionSectionCard.Visibility   = Visibility.Visible;
        VersionSectionHeader.Visibility = Visibility.Visible;
        VersionSeparator.Visibility     = Visibility.Visible;
        VersionSection.Visibility       = Visibility.Visible;
    }

    // Edit mode
    public AddEditOrdinanceWindow(IOrdinanceService service, ApiOrdinance existing)
    {
        InitializeComponent();
        _service = service;
        _existing = existing;
        _isEdit = true;
        WindowTitle.Text = $"✏️ Edit Ordinance — {existing.OrdinanceNumber}";

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
        }
    }

    private void PopulateFields(ApiOrdinance o)
    {
        OrdNumberBox.Text       = o.OrdinanceNumber;
        SeriesBox.Text          = o.SeriesNumber ?? "";
        TitleBox.Text           = o.Title;
        SubjectBox.Text         = o.Subject ?? "";
        SponsorBox.Text         = o.Sponsor ?? "";
        CommitteeBox.Text       = o.Committee ?? "";
        PdfPathBox.Text         = o.DocumentPath ?? "";
        ReferenceNumberBox.Text = o.ReferenceNumber ?? "";
        NrsNsbBox.Text          = o.NrsNsb ?? "";
        NomenclatureBox.Text    = o.Nomenclature ?? "";
        LocationBox.Text        = o.Location ?? "";

        SetComboByContent(TypeCombo, ToTitleCase(o.Type));
        SetComboByContent(StatusCombo, o.Status.ToLowerInvariant() switch
        {
            "in_effect"    => "In Effect",
            "under_review" => "Under Review",
            _              => ToTitleCase(o.Status)
        });
        if (!string.IsNullOrEmpty(o.FinalAction))
            SetComboByContent(FinalActionCombo, o.FinalAction);
        if (!string.IsNullOrEmpty(o.State))
            SetComboByContent(StateCombo, o.State);

        if (o.DatePassed.HasValue)
            DatePassedPicker.SelectedDate = o.DatePassed.Value.ToDateTime(TimeOnly.MinValue);
        if (o.DateApproved.HasValue)
            DateApprovedPicker.SelectedDate = o.DateApproved.Value.ToDateTime(TimeOnly.MinValue);
        if (o.DatePublished.HasValue)
            DatePublishedPicker.SelectedDate = o.DatePublished.Value.ToDateTime(TimeOnly.MinValue);
    }

    private static string ToTitleCase(string s) =>
        s.Length == 0 ? s : char.ToUpper(s[0]) + s[1..];

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

        // Actual upload happens server-side when Save is clicked — this just
        // remembers the local path and shows the filename in the meantime.
        _pickedLocalPdfPath = dlg.FileName;
        PdfPathBox.Text = System.IO.Path.GetFileName(dlg.FileName) + " (will upload on save)";
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private async void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!Validate()) return;
        SaveBtn.IsEnabled = false;
        try
        {
            if (_isEdit) await SaveEditAsync();
            else         await SaveNewAsync();
            DialogResult = true;
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.HandleIfApiFailure(ex))
                return;

            ShowError($"Save failed: {ex.Message}");
        }
        finally
        {
            SaveBtn.IsEnabled = true;
        }
    }

    private async Task SaveNewAsync()
    {
        var input = BuildInput();
        input.InitialVersion = new OrdinanceVersionInput
        {
            Title       = VersionTitleBox.Text.Trim(),
            Content     = VersionContentBox.Text.Trim(),
            EnactedBy   = EnactedByBox.Text.Trim(),
            DateEnacted = DateOnly.FromDateTime(VersionDatePicker.SelectedDate!.Value)
        };

        SavedOrdinance = await _service.AddAsync(input);
    }

    private async Task SaveEditAsync()
    {
        var o = _existing!;
        var input = BuildInput();

        await _service.UpdateAsync(o.Id, input);
        SavedOrdinance = await _service.GetDetailsAsync(o.Id);
    }

    private OrdinanceInput BuildInput() => new()
    {
        OrdinanceNumber   = OrdNumberBox.Text.Trim(),
        SeriesNumber      = NullIfEmpty(SeriesBox.Text),
        Title             = TitleBox.Text.Trim(),
        Subject           = NullIfEmpty(SubjectBox.Text),
        Sponsor           = NullIfEmpty(SponsorBox.Text),
        Committee         = NullIfEmpty(CommitteeBox.Text),
        Type              = ParseType(),
        Status            = ParseStatus(),
        FinalAction       = ParseFinalAction(),
        State             = ParseState(),
        ReferenceNumber   = NullIfEmpty(ReferenceNumberBox.Text),
        NrsNsb            = NullIfEmpty(NrsNsbBox.Text),
        Nomenclature      = NullIfEmpty(NomenclatureBox.Text),
        Location          = NullIfEmpty(LocationBox.Text),
        DatePassed        = DatePassedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DatePassedPicker.SelectedDate.Value) : null,
        DateApproved      = DateApprovedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DateApprovedPicker.SelectedDate.Value) : null,
        DatePublished     = DatePublishedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DatePublishedPicker.SelectedDate.Value) : null,
        LocalDocumentPath = _pickedLocalPdfPath
    };

    private string ParseType() =>
        (TypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
        {
            "Resolution" => "resolution",
            "Minutes"    => "minutes",
            _            => "ordinance"
        };

    private string ParseStatus() =>
        (StatusCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
        {
            "Amended"      => "amended",
            "Superseded"   => "superseded",
            "Repealed"     => "repealed",
            "Under Review" => "under_review",
            _              => "in_effect"
        };

    private string? ParseFinalAction() =>
        (FinalActionCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
        {
            "Approving"   => "approving",
            "Authorizing" => "authorizing",
            "Creating"    => "creating",
            "Declaring"   => "declaring",
            "Conducting"  => "conducting",
            "Extending"   => "extending",
            _             => null
        };

    private string? ParseState() =>
        (StateCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
        {
            "Draft"   => "draft",
            "Passed"  => "passed",
            "Enacted" => "enacted",
            _         => null
        };

    private bool Validate()
    {
        HideError();
        if (_isEdit) return true;

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
        _pickedLocalPdfPath = null;
    }

    private void HideError() => ErrorBanner.Visibility = Visibility.Collapsed;
}