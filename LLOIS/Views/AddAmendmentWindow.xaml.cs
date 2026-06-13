namespace LLOIS.Views;

using System.Windows;
using LLOIS.Models;
using LLOIS.Services;

public partial class AddAmendmentWindow : Window
{
    private readonly IOrdinanceService _service;
    private readonly Ordinance _ordinance;

    public AddAmendmentWindow(IOrdinanceService service, Ordinance ordinance)
    {
        InitializeComponent();
        _service = service;
        _ordinance = ordinance;

        WindowTitle.Text = $"📝 Add Amendment — {ordinance.OrdinanceNumber}";
        ContextOrdNumber.Text = ordinance.OrdinanceNumber;
        ContextTitle.Text = ordinance.Title;
        ContextVersionInfo.Text = $"Currently at Version {ordinance.Versions.Count}. " +
                                  $"This will become Version {ordinance.Versions.Count + 1}.";
        EnactedByBox.Text = ordinance.OrdinanceNumber; // sensible default
    }

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!Validate()) return;

        try
        {
            var version = new OrdinanceVersion
            {
                Title = TitleBox.Text.Trim(),
                Content = ContentBox.Text.Trim(),
                AmendmentNotes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim(),
                EnactedBy = EnactedByBox.Text.Trim(),
                DateEnacted = DateOnly.FromDateTime(DatePicker.SelectedDate!.Value)
            };

            _service.AddAmendment(_ordinance.OrdinanceNumber, version);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Error: {ex.Message}";
            ErrorBanner.Visibility = Visibility.Visible;
        }
    }

    private bool Validate()
    {
        ErrorBanner.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(TitleBox.Text))
            return ShowError("Amendment title is required.");
        if (string.IsNullOrWhiteSpace(ContentBox.Text))
            return ShowError("Content summary is required.");
        if (DatePicker.SelectedDate is null)
            return ShowError("Date Enacted is required.");

        return true;
    }

    private bool ShowError(string msg)
    {
        ErrorText.Text = msg;
        ErrorBanner.Visibility = Visibility.Visible;
        return false;
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
