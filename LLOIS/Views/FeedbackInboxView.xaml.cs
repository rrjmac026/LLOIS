namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using LLOIS.Services;

public partial class FeedbackInboxView : UserControl
{
    private readonly IFeedbackService _service;
    private ApiFeedback? _selected;
    private bool _loaded;
    private int  _loadId;   // guards against out-of-order responses on quick refreshes

    public FeedbackInboxView(IFeedbackService service)
    {
        InitializeComponent();
        _service = service;
    }

    public void Refresh() => _ = LoadAsync();

    public void ReloadIfNeeded()
    {
        if (_loaded) return;
        _loaded = true;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var loadId = ++_loadId;
        try
        {
            var results = (await _service.GetAllAsync()).ToList();
            if (loadId != _loadId) return;   // a newer request superseded this one

            FeedbackList.ItemsSource = results;
            ResultCount.Text = $"{results.Count} feedback item{(results.Count == 1 ? "" : "s")}";
            ClearDetail();
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.HandleIfApiFailure(ex))
                return;

            MessageBox.Show($"Error loading feedback:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FeedbackList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FeedbackList.SelectedItem is not ApiFeedback f) return;
        _selected = f;
        ShowDetail(f);
    }

    private async void ResolveBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) return;

        var id = _selected.Id;
        ResolveBtn.IsEnabled = false;   // block double-clicks while the request is in flight
        try
        {
            await _service.MarkResolvedAsync(id);
            await LoadAsync();
            MessageBox.Show("Marked as resolved.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.HandleIfApiFailure(ex))
                return;

            var detail = ex.InnerException?.Message ?? ex.Message;
            MessageBox.Show($"Failed to update:\n{detail}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ResolveBtn.IsEnabled = true;
        }
    }

    private void ShowDetail(ApiFeedback f)
    {
        DetailPanel.Visibility = Visibility.Visible;
        ActionBar.Visibility   = Visibility.Visible;
        ResolveBtn.Visibility  = string.Equals(f.Status, "open", StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;

        DetailType.Text    = f.Type.ToString();
        DetailMeta.Text    = $"{f.SubmittedBy}  ·  {f.CreatedAt.ToLocalTime():MMMM dd, yyyy h:mm tt}  ·  {f.Status}";
        DetailMessage.Text = f.Message;
    }

    private void ClearDetail()
    {
        DetailPanel.Visibility = Visibility.Collapsed;
        ActionBar.Visibility   = Visibility.Collapsed;
        _selected = null;
    }
}