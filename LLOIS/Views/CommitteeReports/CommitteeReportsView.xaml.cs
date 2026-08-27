namespace LLOIS.Views;

using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LLOIS.Models;
using LLOIS.Services;

public partial class CommitteeReportsView : UserControl
{
    private readonly ICommitteeReportService _service;
    private readonly IAuthService             _auth;
    private readonly User                     _currentUser;
    private string           _searchQuery = string.Empty;
    private CommitteeReport? _selected;
    private bool             _loaded = false;

    private readonly System.Windows.Threading.DispatcherTimer _searchTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(300)
    };

    public CommitteeReportsView(ICommitteeReportService service, IAuthService auth, User user)
    {
        InitializeComponent();
        _service     = service;
        _auth        = auth;
        _currentUser = user;

        bool canWrite = user.Role is UserRole.Admin or UserRole.SuperAdmin or UserRole.Encoder;
        AddBtn.Visibility = canWrite ? Visibility.Visible : Visibility.Collapsed;

        _searchTimer.Tick += SearchTimer_Tick;
    }

    public void ReloadIfNeeded()
    {
        if (!_loaded)
        {
            _loaded = true;
            _ = LoadAsync();
        }
    }

    public void Refresh() => _ = LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            var results = await Task.Run(() => _service.Search(_searchQuery).ToList());
            ReportList.ItemsSource = results;
            ResultCount.Text = $"{results.Count} report{(results.Count == 1 ? "" : "s")} found";
            ClearDetail();
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                return;

            MessageBox.Show($"Error loading committee reports:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        _searchQuery = SearchBox.Text.Trim();
        await LoadAsync();
    }

    private void ReportList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReportList.SelectedItem is not CommitteeReport r) return;
        _selected = r;
        ShowDetail(r);
    }

    private void AddBtn_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AddCommitteeReportWindow(_service, _currentUser) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            _ = Task.Run(() => _auth.LogAction(_currentUser, "ADD",
                $"Added committee report {dlg.SavedReport?.ReportNumber}"));
            _loaded = false;
            ReloadIfNeeded();

            MessageBox.Show("Committee report created successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void EditBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) return;
        var dlg = new AddCommitteeReportWindow(_service, _currentUser, _selected) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            _ = Task.Run(() => _auth.LogAction(_currentUser, "EDIT",
                $"Edited committee report {_selected.ReportNumber}"));
            await LoadAsync();

            MessageBox.Show("Committee report updated successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) return;
        var result = MessageBox.Show(
            $"Permanently delete report {_selected.ReportNumber}?\n\nThis cannot be undone.",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            var num = _selected.ReportNumber;
            await Task.Run(() => _service.Delete(_selected.Id));
            _ = Task.Run(() => _auth.LogAction(_currentUser, "DELETE",
                $"Deleted committee report {num}"));
            await LoadAsync();

            MessageBox.Show("Committee report deleted successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                return;

            MessageBox.Show($"Delete failed:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenAttachment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string path } || string.IsNullOrEmpty(path)) return;

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open file:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowDetail(CommitteeReport r)
    {
        DetailPanel.Visibility = Visibility.Visible;
        ActionBar.Visibility   = Visibility.Visible;

        bool canWrite = _currentUser.Role is UserRole.Admin or UserRole.Encoder;
        bool isAdmin  = _currentUser.Role == UserRole.Admin;

        EditBtn.Visibility   = canWrite ? Visibility.Visible : Visibility.Collapsed;
        DeleteBtn.Visibility = canWrite  ? Visibility.Visible : Visibility.Collapsed;
        InlineActionRow.Visibility  = canWrite ? Visibility.Visible : Visibility.Collapsed;
        InlineEditBtn.Visibility    = canWrite ? Visibility.Visible : Visibility.Collapsed;
        AttachmentBadge.Visibility  = r.Attachments.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        DetailNumber.Text  = r.ReportNumber;
        DetailSubject.Text = r.Subject;
        DetailDate.Text    = r.Date?.ToString("MMMM dd, yyyy") ?? "No date set";

        MetadataGrid.Children.Clear();
        AddMetaCell(r.SubmittedBy, "Submitted by");
        AddMetaCell(r.SponsoredBy, "Sponsored by");
        AddMetaCell(r.Date?.ToString("MMMM dd, yyyy") ?? "—", "Date");
        if (!string.IsNullOrEmpty(r.AddedBy))
        AddMetaCell(r.AddedBy, "Added by");

        AttachmentsList.ItemsSource = r.Attachments;
    }

    private void ClearDetail()
    {
        DetailPanel.Visibility = Visibility.Collapsed;
        ActionBar.Visibility   = Visibility.Collapsed;
        _selected = null;
    }

    private void AddMetaCell(string value, string label)
    {
        var cell = new Border { Margin = new Thickness(0,0,8,8), CornerRadius = new CornerRadius(8), Padding = new Thickness(12,10,12,10) };
        cell.SetResourceReference(Border.BackgroundProperty, "BgSecondaryBrush");
        var inner = new StackPanel();
        var key = new TextBlock { Text = label.ToUpper(), FontSize = 10, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0,0,0,3) };
        key.SetResourceReference(TextBlock.ForegroundProperty, "TextTertiaryBrush");
        inner.Children.Add(key);
        var val = new TextBlock { Text = string.IsNullOrEmpty(value) ? "—" : value, FontSize = 12, FontWeight = FontWeights.Medium, TextWrapping = TextWrapping.Wrap };
        val.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        inner.Children.Add(val);
        cell.Child = inner;
        MetadataGrid.Children.Add(cell);
    }

    private async void SearchTimer_Tick(object? sender, EventArgs e)
    {
        _searchTimer.Stop();
        _searchQuery = SearchBox.Text.Trim();
        await LoadAsync();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchTimer.Stop();
        _searchTimer.Start();
    }
}