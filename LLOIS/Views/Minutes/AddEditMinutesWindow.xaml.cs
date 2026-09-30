namespace LLOIS.Views;

using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using LLOIS.Services;

public partial class AddEditMinutesWindow : Window
{
    // Laravel validates `document` as pdf/doc/docx, max 20480 KB.
    private const long MaxFileBytes = 20L * 1024 * 1024;

    private readonly IMinutesService _service;
    private readonly ApiMinutes? _existing;
    private readonly bool _isEdit;

    // Newly picked file, uploaded together with the form on Save.
    private string? _localDocumentPath;

    // Only set in Add mode (the created record as returned by the API).
    public ApiMinutes? SavedMinutes { get; private set; }

    // Add mode
    public AddEditMinutesWindow(IMinutesService service)
    {
        InitializeComponent();
        _service = service;
        _isEdit = false;
        WindowTitle.Text = "➕ Add Minutes";
    }

    // Edit mode
    public AddEditMinutesWindow(IMinutesService service, ApiMinutes existing)
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

    // ── File selection (upload happens on Save) ─────────────────

    private void BrowseFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Minutes Document",
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

    private async void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!Validate()) return;

        SaveBtn.IsEnabled = false;
        try
        {
            var input = new MinutesInput
            {
                SessionType = (SessionTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "",
                Date = DatePickerControl.SelectedDate.HasValue
                    ? DateOnly.FromDateTime(DatePickerControl.SelectedDate.Value) : null,
                LocalDocumentPath = _localDocumentPath
            };

            if (_isEdit)
                await _service.UpdateAsync(_existing!.Id, input);
            else
                SavedMinutes = await _service.AddAsync(input);

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