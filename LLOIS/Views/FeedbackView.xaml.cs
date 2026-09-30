namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using LLOIS.Services;

public partial class FeedbackView : UserControl
{
    private readonly IFeedbackService _service;

    public FeedbackView(IFeedbackService service)
    {
        InitializeComponent();
        _service = service;
    }

    private async void SubmitBtn_Click(object sender, RoutedEventArgs e)
    {
        SuccessBanner.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(MessageTextBox.Text))
        {
            MessageBox.Show("Please enter a message.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var type = (TypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
        {
            "Bug"        => ApiFeedbackType.Bug,
            "Suggestion" => ApiFeedbackType.Suggestion,
            _            => ApiFeedbackType.Concern
        };

        var button = (Button)sender;
        button.IsEnabled = false;   // block double-submits while the request is in flight
        try
        {
            // The server fills in submitted_by (from the token) and created_at.
            await _service.SubmitAsync(type, MessageTextBox.Text.Trim());

            MessageTextBox.Text = "";
            TypeCombo.SelectedIndex = 0;
            SuccessBanner.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            if (!ConnectionFailureHandler.HandleIfApiFailure(ex))
                MessageBox.Show($"Failed to submit: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            button.IsEnabled = true;
        }
    }
}