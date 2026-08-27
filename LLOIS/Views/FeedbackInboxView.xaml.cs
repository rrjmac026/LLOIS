namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using LLOIS.Models;
using LLOIS.Services;

public partial class FeedbackInboxView : UserControl
{
    private readonly IFeedbackService _service;
    private Feedback? _selected;
    private bool _loaded = false;

    public FeedbackInboxView(IFeedbackService service)
    {
        InitializeComponent();
        _service = service;
    }

    public void Refresh() => Load();

    public void ReloadIfNeeded()
    {
        if (!_loaded) { _loaded = true; Load(); }
    }

    private void Load()
    {
        try
        {
            var results = _service.GetAll().ToList();
            FeedbackList.ItemsSource = results;
            ResultCount.Text = $"{results.Count} feedback item{(results.Count == 1 ? "" : "s")}";
            ClearDetail();
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                return;

            MessageBox.Show($"Error loading feedback:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FeedbackList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FeedbackList.SelectedItem is not Feedback f) return;
        _selected = f;
        ShowDetail(f);
    }

    private void ResolveBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) return;
        try
        {
            _service.MarkResolved(_selected.Id);
            Load();
            MessageBox.Show("Marked as resolved.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                return;

            var detail = ex.InnerException?.Message ?? ex.Message;
            MessageBox.Show($"Failed to update:\n{detail}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    

    private void ShowDetail(Feedback f)
    {
        DetailPanel.Visibility = Visibility.Visible;
        ActionBar.Visibility   = Visibility.Visible;
        ResolveBtn.Visibility  = f.Status == FeedbackStatus.Open ? Visibility.Visible : Visibility.Collapsed;

        DetailType.Text    = f.Type.ToString();
        DetailMeta.Text    = $"{f.SubmittedBy}  ·  {f.CreatedAt:MMMM dd, yyyy h:mm tt}  ·  {f.Status}";
        DetailMessage.Text = f.Message;
    }

    private void ClearDetail()
    {
        DetailPanel.Visibility = Visibility.Collapsed;
        ActionBar.Visibility   = Visibility.Collapsed;
        _selected = null;
    }
}