namespace LLOIS.Views;

using System.IO;
using System.Windows;
using Microsoft.Win32;
using LLOIS.Services;

public partial class AddEditResolutionWindow : Window
{
    // Laravel validates `document` as pdf/doc/docx, max 20480 KB.
    private const long MaxFileBytes = 20L * 1024 * 1024;

    private readonly IResolutionService _service;
    private readonly ApiResolution? _existing;
    private readonly bool _isEdit;

    // Newly picked file, uploaded together with the form on Save.
    private string? _localDocumentPath;

    // Only set in Add mode (the created resolution as returned by the API).
    public ApiResolution? SavedResolution { get; private set; }

    // Add mode
    public AddEditResolutionWindow(IResolutionService service)
    {
        InitializeComponent();
        _service = service;
        _isEdit = false;
        WindowTitle.Text = "➕ Add New Resolution";
    }

    // Edit mode
    public AddEditResolutionWindow(IResolutionService service, ApiResolution existing)
    {
        InitializeComponent();
        _service = service;
        _existing = existing;
        _isEdit = true;
        WindowTitle.Text = $"✏️ Edit Resolution — {existing.ResolutionNumber}";

        PopulateFields(existing);
    }

    private void PopulateFields(ApiResolution r)
    {
        ResNumberBox.Text   = r.ResolutionNumber;
        SbTermBox.Text      = r.SbTerm ?? "";
        SessionInfoBox.Text = r.SessionInfo ?? "";
        CommitteeBox.Text   = r.Committee ?? "";
        TitleBox.Text       = r.Title;
        SponsorBox.Text     = r.Sponsor ?? "";
        FilePathBox.Text    = r.DocumentPath ?? "";

        if (r.DateApproved.HasValue)
            DateApprovedPicker.SelectedDate = r.DateApproved.Value.ToDateTime(TimeOnly.MinValue);
    }

    // ── File selection (upload happens on Save) ─────────────────

    private void BrowseFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Resolution Document",
            Filter = "Documents (*.pdf;*.doc;*.docx)|*.pdf;*.doc;*.docx"
        };
        if (dlg.ShowDialog() != true) return;

        var info = new FileInfo(dlg.FileName);
        if (info.Length > MaxFileBytes)
        {
            ShowError("File is too large. Maximum size is 20 MB.");
            return;
        }

        HideError();
        _localDocumentPath = dlg.FileName;
        FilePathBox.Text   = info.Name;
    }

    private void RemoveFileBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_localDocumentPath is not null)
        {
            // Undo the pick and fall back to whatever is already on the server.
            _localDocumentPath = null;
            FilePathBox.Text   = _existing?.DocumentPath ?? "";
            return;
        }

        if (!string.IsNullOrEmpty(_existing?.DocumentPath))
            MessageBox.Show(
                "Removing an existing document isn't supported by the API yet.\nPick a new file to replace it.",
                "Not supported", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ── Save ─────────────────────────────────────────────────────

    private void CancelBtn_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private async void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!Validate()) return;

        SaveBtn.IsEnabled = false;
        try
        {
            var input = BuildInput();

            if (_isEdit)
                await _service.UpdateAsync(_existing!.Id, input);
            else
                SavedResolution = await _service.AddAsync(input);

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

    private ResolutionInput BuildInput() => new()
    {
        ResolutionNumber  = ResNumberBox.Text.Trim(),
        SbTerm            = NullIfEmpty(SbTermBox.Text),
        SessionInfo       = NullIfEmpty(SessionInfoBox.Text),
        Committee         = NullIfEmpty(CommitteeBox.Text),
        Title             = TitleBox.Text.Trim(),
        Sponsor           = NullIfEmpty(SponsorBox.Text),
        DateApproved      = DateApprovedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DateApprovedPicker.SelectedDate.Value) : null,
        LocalDocumentPath = _localDocumentPath
    };

    private bool Validate()
    {
        HideError();
        if (string.IsNullOrWhiteSpace(ResNumberBox.Text)) return ShowError("Resolution Number is required.");
        if (string.IsNullOrWhiteSpace(TitleBox.Text))     return ShowError("Title is required.");
        if (string.IsNullOrWhiteSpace(SponsorBox.Text))   return ShowError("Sponsor is required.");
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