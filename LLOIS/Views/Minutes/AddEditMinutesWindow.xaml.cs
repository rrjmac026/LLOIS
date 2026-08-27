namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using LLOIS.Models;
using LLOIS.Services;

public partial class AddEditMinutesWindow : Window
{
    private readonly IMinutesService _service;
    private readonly Minutes? _existing;
    private readonly bool _isEdit;

    public Minutes? SavedMinutes { get; private set; }

    // Add mode
    public AddEditMinutesWindow(IMinutesService service)
    {
        InitializeComponent();
        _service = service;
        _isEdit = false;
        WindowTitle.Text = "➕ Add Minutes";
    }

    // Edit mode
    public AddEditMinutesWindow(IMinutesService service, Minutes existing)
    {
        InitializeComponent();
        _service = service;
        _existing = existing;
        _isEdit = true;
        WindowTitle.Text = "✏️ Edit Minutes";

        SetComboByContent(SessionTypeCombo, existing.SessionType);
        if (existing.Date.HasValue)
            DatePickerControl.SelectedDate = existing.Date.Value.ToDateTime(TimeOnly.MinValue);
        FilePathBox.Text = existing.DocumentPath ?? "";
    }

    private static void SetComboByContent(ComboBox combo, string content)
    {
        foreach (ComboBoxItem item in combo.Items)
        {
            if (item.Content?.ToString() == content)
            { combo.SelectedItem = item; return; }
        }
    }

    private void BrowseFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Minutes Document",
            Filter = "All Files (*.*)|*.*"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            FilePathBox.Text = "Uploading...";
            var url = StorageService.UploadMinutesFile(dlg.FileName);
            FilePathBox.Text = url;
        }
        catch (Exception ex)
        {
            FilePathBox.Text = "";
            if (!ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                MessageBox.Show($"Upload failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RemoveFileBtn_Click(object sender, RoutedEventArgs e)
    {
        FilePathBox.Text = "";
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!Validate()) return;
        try
        {
            var sessionType = (SessionTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            var date = DatePickerControl.SelectedDate.HasValue
                ? DateOnly.FromDateTime(DatePickerControl.SelectedDate.Value) : (DateOnly?)null;
            var docPath = string.IsNullOrWhiteSpace(FilePathBox.Text) ? null : FilePathBox.Text.Trim();

            if (_isEdit)
            {
                var m = _existing!;
                m.SessionType = sessionType;
                m.Date = date;
                m.DocumentPath = docPath;
                _service.Update(m);
                SavedMinutes = m;
            }
            else
            {
                var m = new Minutes
                {
                    SessionType = sessionType,
                    Date = date,
                    DocumentPath = docPath
                };
                _service.Add(m);
                SavedMinutes = m;
            }

            DialogResult = true;
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                return;

            ShowError($"Save failed: {ex.Message}");
        }
    }

    private bool Validate()
    {
        HideError();
        if (SessionTypeCombo.SelectedItem is null)
            return ShowError("Session Type is required.");
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