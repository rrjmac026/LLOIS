namespace LLOIS.Views;

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using LLOIS.Models;
using LLOIS.Services;

public partial class AddCommitteeReportWindow : Window
{
    private readonly ICommitteeReportService _service;
    private readonly User _currentUser;
    private int? _editingId;
    private string? _existingAddedBy;
    private DateTime? _existingAddedAt;

    // Unified display list — holds both existing (already-uploaded) attachments
    // and newly-picked local files not yet uploaded.
    private readonly ObservableCollection<AttachmentEntry> _attachments = [];

    public CommitteeReport? SavedReport { get; private set; }

    public AddCommitteeReportWindow(ICommitteeReportService service, User currentUser)
    {
        InitializeComponent();
        _service = service;
        _currentUser = currentUser;
        AttachmentsList.ItemsSource = _attachments;
    }

    public AddCommitteeReportWindow(ICommitteeReportService service, User currentUser, CommitteeReport existing)
        : this(service, currentUser)
    {
        _editingId = existing.Id;
        _existingAddedBy = existing.AddedBy;
        _existingAddedAt = existing.AddedAt;
        ReportNumberBox.Text  = existing.ReportNumber;
        DatePickerControl.SelectedDate = existing.Date?.ToDateTime(TimeOnly.MinValue);
        SubmittedByBox.Text   = existing.SubmittedBy;
        SponsoredByBox.Text   = existing.SponsoredBy;
        SubjectBox.Text       = existing.Subject;

        foreach (var a in existing.Attachments)
            _attachments.Add(new AttachmentEntry { FileName = a.FileName, ExistingUrl = a.FilePath });
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

        var report = new CommitteeReport
        {
            Id           = _editingId ?? 0,
            ReportNumber = ReportNumberBox.Text.Trim(),
            Date         = DatePickerControl.SelectedDate.HasValue
                            ? DateOnly.FromDateTime(DatePickerControl.SelectedDate.Value)
                            : null,
            SubmittedBy  = SubmittedByBox.Text.Trim(),
            SponsoredBy  = SponsoredByBox.Text.Trim(),
            Subject      = SubjectBox.Text.Trim(),
            AddedBy      = _editingId.HasValue ? _existingAddedBy : _currentUser.Username,
            AddedAt      = _editingId.HasValue ? _existingAddedAt : DateTime.UtcNow
        };

        try
        {
            foreach (var entry in _attachments)
            {
                if (entry.ExistingUrl is not null)
                {
                    // Already uploaded — carry it over unchanged
                    report.Attachments.Add(new CommitteeReportAttachment
                    {
                        FileName = entry.FileName,
                        FilePath = entry.ExistingUrl
                    });
                }
                else if (entry.LocalPath is not null)
                {
                    // Newly picked — upload now
                    var url = StorageService.UploadCommitteeReportFile(entry.LocalPath);
                    report.Attachments.Add(new CommitteeReportAttachment
                    {
                        FileName = entry.FileName,
                        FilePath = url
                    });
                }
            }

            if (_editingId.HasValue)
                _service.Update(report);
            else
                _service.Add(report);

            SavedReport = report;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;

            if (!ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                MessageBox.Show($"Failed to save report:\n{detail}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SaveBtn.IsEnabled = true;
        }
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private class AttachmentEntry
    {
        public string FileName { get; set; } = string.Empty;
        public string? LocalPath { get; set; }     // set for newly-picked, not-yet-uploaded files
        public string? ExistingUrl { get; set; }   // set for already-uploaded attachments (edit mode)
    }
}