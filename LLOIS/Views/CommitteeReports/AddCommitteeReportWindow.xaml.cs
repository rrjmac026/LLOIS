namespace LLOIS.Views;

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using LLOIS.Services;

public partial class AddCommitteeReportWindow : Window
{
    private readonly ICommitteeReportService _service;
    private readonly ApiUser _currentUser;
    private int? _editingId;

    // Ids of attachments that existed on the server when this window opened —
    // used to detect which ones the user removed, so we can call
    // DeleteAttachmentAsync for those specifically.
    private readonly List<int> _originalAttachmentIds = [];

    // Unified display list — holds both existing (already-uploaded) attachments
    // and newly-picked local files not yet uploaded.
    private readonly ObservableCollection<AttachmentEntry> _attachments = [];

    public ApiCommitteeReport? SavedReport { get; private set; }

    public AddCommitteeReportWindow(ICommitteeReportService service, ApiUser currentUser)
    {
        InitializeComponent();
        _service = service;
        _currentUser = currentUser;
        AttachmentsList.ItemsSource = _attachments;
    }

    public AddCommitteeReportWindow(ICommitteeReportService service, ApiUser currentUser, ApiCommitteeReportDetails existing)
        : this(service, currentUser)
    {
        _editingId = existing.Report.Id;
        ReportNumberBox.Text  = existing.Report.ReportNumber;
        DatePickerControl.SelectedDate = existing.Report.Date?.ToDateTime(TimeOnly.MinValue);
        SubmittedByBox.Text   = existing.Report.SubmittedBy;
        SponsoredByBox.Text   = existing.Report.SponsoredBy;
        SubjectBox.Text       = existing.Report.Subject;

        foreach (var a in existing.Attachments)
        {
            _originalAttachmentIds.Add(a.Id);
            _attachments.Add(new AttachmentEntry
            {
                Id = a.Id,
                FileName = a.FileName,
                ExistingUrl = a.Url ?? a.FilePath
            });
        }
    }

    private void AddFilesBtn_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "All Files (*.*)|*.*"
        };
        if (dlg.ShowDialog() != true) return;

        foreach (var path in dlg.FileNames)
        {
            _attachments.Add(new AttachmentEntry
            {
                FileName = Path.GetFileName(path),
                LocalPath = path
            });
        }
    }

    private void RemoveAttachment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: AttachmentEntry entry })
            _attachments.Remove(entry);
    }

    private async void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ReportNumberBox.Text))
        {
            MessageBox.Show("Report number is required.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SaveBtn.IsEnabled = false;

        var input = new CommitteeReportInput
        {
            ReportNumber = ReportNumberBox.Text.Trim(),
            Date         = DatePickerControl.SelectedDate.HasValue
                            ? DateOnly.FromDateTime(DatePickerControl.SelectedDate.Value)
                            : null,
            SubmittedBy  = SubmittedByBox.Text.Trim(),
            SponsoredBy  = SponsoredByBox.Text.Trim(),
            Subject      = SubjectBox.Text.Trim(),
            NewAttachmentFilePaths = _attachments
                .Where(a => a.LocalPath is not null)
                .Select(a => a.LocalPath!)
                .ToList()
        };

        try
        {
            if (_editingId.HasValue)
            {
                // Remove attachments the user deleted from the list while editing
                var remainingIds = _attachments.Where(a => a.Id.HasValue).Select(a => a.Id!.Value).ToHashSet();
                foreach (var removedId in _originalAttachmentIds.Where(id => !remainingIds.Contains(id)))
                    await _service.DeleteAttachmentAsync(_editingId.Value, removedId);

                await _service.UpdateAsync(_editingId.Value, input);
                SavedReport = new ApiCommitteeReport { Id = _editingId.Value, ReportNumber = input.ReportNumber };
            }
            else
            {
                SavedReport = await _service.AddAsync(input);
            }

            DialogResult = true;
        }
        catch (Exception ex)
        {
            if (!ConnectionFailureHandler.HandleIfApiFailure(ex))
                MessageBox.Show($"Failed to save: {ex.Message}", "Error");
        }
        finally
        {
            SaveBtn.IsEnabled = true;
        }
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private class AttachmentEntry
    {
        public int? Id { get; set; }               // set for already-uploaded attachments (edit mode)
        public string FileName { get; set; } = string.Empty;
        public string? LocalPath { get; set; }      // set for newly-picked, not-yet-uploaded files
        public string? ExistingUrl { get; set; }    // set for already-uploaded attachments (edit mode)
    }
}