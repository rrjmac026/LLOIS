namespace LLOIS.Views;

using System.Windows;
using Microsoft.Win32;
using LLOIS.Models;
using LLOIS.Services;

public partial class AddEditResolutionWindow : Window
{
    private readonly IResolutionService _service;
    private readonly User _currentUser;
    private readonly Resolution? _existing;
    private readonly bool _isEdit;

    public Resolution? SavedResolution { get; private set; }

    // Add mode
    public AddEditResolutionWindow(IResolutionService service, User currentUser)
    {
        InitializeComponent();
        _service = service;
        _currentUser = currentUser;
        _isEdit = false;
        WindowTitle.Text = "➕ Add New Resolution";
    }

    // Edit mode
    public AddEditResolutionWindow(IResolutionService service, User currentUser, Resolution existing)
    {
        InitializeComponent();
        _service = service;
        _currentUser = currentUser;
        _existing = existing;
        _isEdit = true;
        WindowTitle.Text = $"✏️ Edit Resolution — {existing.ResolutionNumber}";

        PopulateFields(existing);
    }

    private void PopulateFields(Resolution r)
    {
        ResNumberBox.Text     = r.ResolutionNumber;
        SbTermBox.Text        = r.SbTerm;
        SessionInfoBox.Text   = r.SessionInfo;
        CommitteeBox.Text     = r.Committee;
        TitleBox.Text         = r.Title;
        SponsorBox.Text       = r.Sponsor;
        FilePathBox.Text      = r.DocumentPath ?? "";

        if (r.DateApproved.HasValue)
            DateApprovedPicker.SelectedDate = r.DateApproved.Value.ToDateTime(TimeOnly.MinValue);
    }

    // ── File upload ──────────────────────────────────────────────

    private void BrowseFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Resolution Document",
            Filter = "All Files (*.*)|*.*"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            FilePathBox.Text = "Uploading...";
            var url = StorageService.UploadResolutionFile(dlg.FileName);
            FilePathBox.Text = url;
        }
        catch (Exception ex)
        {
            FilePathBox.Text = "";
            if (!ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                MessageBox.Show($"Upload failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Save ─────────────────────────────────────────────────────

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
        var resolution = BuildResolution();
        _service.Add(resolution);
        SavedResolution = resolution;
    }

    private void SaveEdit()
    {
        var r = _existing!;
        r.ResolutionNumber  = ResNumberBox.Text.Trim();
        r.SbTerm            = SbTermBox.Text.Trim();
        r.SessionInfo       = SessionInfoBox.Text.Trim();
        r.Committee         = CommitteeBox.Text.Trim();
        r.Title             = TitleBox.Text.Trim();
        r.Sponsor           = SponsorBox.Text.Trim();
        r.DateApproved      = DateApprovedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DateApprovedPicker.SelectedDate.Value) : null;
        r.DocumentPath      = NullIfEmpty(FilePathBox.Text);

        _service.Update(r);
        SavedResolution = r;
    }

    private Resolution BuildResolution() => new()
    {
        ResolutionNumber = ResNumberBox.Text.Trim(),
        SbTerm           = SbTermBox.Text.Trim(),
        SessionInfo      = SessionInfoBox.Text.Trim(),
        Committee        = CommitteeBox.Text.Trim(),
        Title            = TitleBox.Text.Trim(),
        Sponsor          = SponsorBox.Text.Trim(),
        DateApproved     = DateApprovedPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DateApprovedPicker.SelectedDate.Value) : null,
        DocumentPath     = NullIfEmpty(FilePathBox.Text),
        AddedBy          = _currentUser.Username,
        AddedAt          = DateTime.UtcNow
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

    private void RemoveFileBtn_Click(object sender, RoutedEventArgs e)
    {
        FilePathBox.Text = "";
    }

    private void HideError() => ErrorBanner.Visibility = Visibility.Collapsed;
}