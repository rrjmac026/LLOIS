namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using LLOIS.Models;
using LLOIS.Services;

public partial class FeedbackView : UserControl
{
    private readonly IFeedbackService _service;
    private readonly User _currentUser;

    public FeedbackView(IFeedbackService service, User currentUser)
    {
        InitializeComponent();
        _service = service;
        _currentUser = currentUser;
    }

    private void SubmitBtn_Click(object sender, RoutedEventArgs e)
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
            "Bug"        => FeedbackType.Bug,
            "Suggestion" => FeedbackType.Suggestion,
            _            => FeedbackType.Concern
        };

        try
        {
            _service.Submit(new Feedback
            {
                SubmittedBy = _currentUser.Username,
                Type        = type,
                Message     = MessageTextBox.Text.Trim(),
                CreatedAt   = DateTime.UtcNow
            });

            MessageTextBox.Text = "";
            TypeCombo.SelectedIndex = 0;
            SuccessBanner.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            if (!ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                MessageBox.Show($"Failed to submit: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}