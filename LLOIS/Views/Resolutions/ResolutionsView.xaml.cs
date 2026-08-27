namespace LLOIS.Views;

using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LLOIS.Models;
using LLOIS.Services;

public partial class ResolutionsView : UserControl
{
    private readonly IResolutionService _service;
    private readonly IAuthService       _auth;
    private readonly User               _currentUser;
    private string      _searchQuery      = string.Empty;
    private Resolution? _selectedResolution;
    private bool        _loaded           = false;

    public void Refresh() => _ = LoadResolutionsAsync();

    private readonly System.Windows.Threading.DispatcherTimer _searchTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(300)
    };

    public ResolutionsView(IResolutionService service, IAuthService auth, User user)
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
            _ = LoadResolutionsAsync();
        }
    }

    private async Task LoadResolutionsAsync()
    {
        try
        {
            var query   = _searchQuery;
            var results = await Task.Run(() => _service.Search(query).ToList());

            ResolutionList.ItemsSource = results;
            ResultCount.Text = $"{results.Count} resolutions found";
            ClearDetail();
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                return;

            MessageBox.Show($"Error loading resolutions:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        _searchQuery = SearchBox.Text.Trim();
        await LoadResolutionsAsync();
    }

    private void ResolutionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResolutionList.SelectedItem is not Resolution r) return;

        try
        {
            var detail = _service.GetDetails(r.Id);
            if (detail is null) return;

            _selectedResolution = detail;
            ShowDetail(detail);
            _ = Task.Run(() =>
            {
                try { _auth.LogAction(_currentUser, "VIEW", $"Viewed resolution {r.ResolutionNumber}"); }
                catch { /* non-critical */ }
            });
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                return;

            MessageBox.Show($"Error loading resolution details:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddBtn_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AddEditResolutionWindow(_service, _currentUser) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            var num = dlg.SavedResolution?.ResolutionNumber;
            _ = Task.Run(() =>
            {
                try { _auth.LogAction(_currentUser, "ADD", $"Added resolution {num}"); }
                catch { /* non-critical */ }
            });
            _loaded = false;
            ReloadIfNeeded();

            MessageBox.Show("Resolution created successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void EditBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedResolution is null) return;

        var dlg = new AddEditResolutionWindow(_service, _currentUser, _selectedResolution) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            var num = _selectedResolution.ResolutionNumber;
            _ = Task.Run(() =>
            {
                try { _auth.LogAction(_currentUser, "EDIT", $"Edited resolution {num}"); }
                catch { /* non-critical */ }
            });

            await LoadResolutionsAsync();

            var updated = _service.GetDetails(_selectedResolution.Id);
            if (updated is not null)
            {
                _selectedResolution = updated;
                ShowDetail(updated);
            }

            MessageBox.Show("Resolution updated successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OpenFileBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedResolution?.DocumentPath is null) return;

        try
        {
            Process.Start(new ProcessStartInfo(_selectedResolution.DocumentPath)
                { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open file:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedResolution is null) return;
        var result = MessageBox.Show(
            $"Permanently delete Resolution No. {_selectedResolution.ResolutionNumber}?\n\nThis cannot be undone.",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            var id = _selectedResolution.Id;
            var num = _selectedResolution.ResolutionNumber;
            await Task.Run(() => _service.Delete(id));
            _ = Task.Run(() =>
            {
                try { _auth.LogAction(_currentUser, "DELETE", $"Deleted resolution {num}"); }
                catch { /* non-critical */ }
            });
            await LoadResolutionsAsync();

            MessageBox.Show("Resolution deleted successfully.", "Success",
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

    private void ShowDetail(Resolution r)
    {
        DetailPanel.Visibility = Visibility.Visible;

        bool canWrite = _currentUser.Role is UserRole.Admin or UserRole.Encoder;
        bool isAdmin  = _currentUser.Role == UserRole.Admin;

        ActionBar.Visibility = canWrite ? Visibility.Visible : Visibility.Collapsed;
        DeleteBtn.Visibility = canWrite ? Visibility.Visible : Visibility.Collapsed;

        InlineActionRow.Visibility  = canWrite ? Visibility.Visible : Visibility.Collapsed;
        InlineEditBtn.Visibility    = canWrite ? Visibility.Visible : Visibility.Collapsed;
        InlineOpenFileBtn.Visibility = r.DocumentPath is not null ? Visibility.Visible : Visibility.Collapsed;

        DetailId.Text      = $"Resolution No. {r.ResolutionNumber}  ·  {r.SbTerm}";
        DetailTitle.Text   = r.Title;
        DetailSubtext.Text = $"{r.SessionInfo}  ·  Sponsor: {r.Sponsor}";

        PdfBadge.Visibility = !string.IsNullOrEmpty(r.DocumentPath) ? Visibility.Visible : Visibility.Collapsed;

        MetadataGrid.Children.Clear();
        AddMetaCell(r.Committee, "Committee");
        AddMetaCell(r.DateApproved?.ToString("MMMM dd, yyyy") ?? "—", "Date approved");
        if (!string.IsNullOrEmpty(r.AddedBy))
            AddMetaCell(r.AddedBy, "Added by");
    }

    private void ClearDetail()
    {
        DetailPanel.Visibility = Visibility.Collapsed;
        ActionBar.Visibility   = Visibility.Collapsed;
        _selectedResolution    = null;
    }

    private void AddMetaCell(string value, string label)
    {
        var cell = new Border { Margin = new Thickness(0,0,8,8), CornerRadius = new CornerRadius(8), Padding = new Thickness(12,10,12,10) };
        cell.SetResourceReference(Border.BackgroundProperty, "BgSecondaryBrush");
        var inner = new StackPanel();
        var key = new TextBlock { Text = label.ToUpper(), FontSize = 10, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0,0,0,3) };
        key.SetResourceReference(TextBlock.ForegroundProperty, "TextTertiaryBrush");
        inner.Children.Add(key);
        var val = new TextBlock { Text = value, FontSize = 12, FontWeight = FontWeights.Medium, TextWrapping = TextWrapping.Wrap };
        val.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        inner.Children.Add(val);
        cell.Child = inner;
        MetadataGrid.Children.Add(cell);
    }

    private async void SearchTimer_Tick(object? sender, EventArgs e)
    {
        _searchTimer.Stop();
        _searchQuery = SearchBox.Text.Trim();
        await LoadResolutionsAsync();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchTimer.Stop();
        _searchTimer.Start();
    }
}