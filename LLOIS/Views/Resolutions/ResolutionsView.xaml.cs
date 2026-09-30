namespace LLOIS.Views;

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LLOIS.Services;

public partial class ResolutionsView : UserControl
{
    private readonly IResolutionService _service;
    private readonly ApiUser            _currentUser;
    private string          _searchQuery        = string.Empty;
    private ApiResolution?  _selectedResolution;
    private bool            _loaded             = false;

    // RoleName comes straight from Laravel's UserRole enum (login response "role_name").
    private bool CanWrite => _currentUser.RoleName is "Admin" or "SuperAdmin" or "Encoder";

    public void Refresh() => _ = LoadResolutionsAsync();

    private readonly System.Windows.Threading.DispatcherTimer _searchTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(300)
    };

    // `auth` is kept only so the existing call in MainView still compiles.
    // Audit logging now happens server-side, so it's unused here — remove it
    // from the constructor and from MainView whenever convenient.
    public ResolutionsView(IResolutionService service, IAuthService auth, ApiUser user)
    {
        InitializeComponent();
        _service     = service;
        _currentUser = user;

        AddBtn.Visibility = CanWrite ? Visibility.Visible : Visibility.Collapsed;

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
            var results = (await _service.SearchAsync(_searchQuery)).ToList();

            ResolutionList.ItemsSource = results;
            ResultCount.Text = $"{results.Count} resolutions found";
            ClearDetail();
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.HandleIfApiFailure(ex))
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

    private async void ResolutionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResolutionList.SelectedItem is not ApiResolution r) return;

        try
        {
            var detail = await _service.GetDetailsAsync(r.Id);
            if (detail is null) return;

            _selectedResolution = detail;
            ShowDetail(detail);
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.HandleIfApiFailure(ex))
                return;

            MessageBox.Show($"Error loading resolution details:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void AddBtn_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AddEditResolutionWindow(_service) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            await LoadResolutionsAsync();

            MessageBox.Show("Resolution created successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void EditBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedResolution is null) return;

        var id  = _selectedResolution.Id;
        var dlg = new AddEditResolutionWindow(_service, _selectedResolution) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() != true) return;

        try
        {
            await LoadResolutionsAsync();

            var updated = await _service.GetDetailsAsync(id);
            if (updated is not null)
            {
                _selectedResolution = updated;
                ShowDetail(updated);
            }

            MessageBox.Show("Resolution updated successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.HandleIfApiFailure(ex))
                return;

            MessageBox.Show($"Error refreshing resolution:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
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
            await _service.DeleteAsync(_selectedResolution.Id);
            await LoadResolutionsAsync();

            MessageBox.Show("Resolution deleted successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.HandleIfApiFailure(ex))
                return;

            MessageBox.Show($"Delete failed:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowDetail(ApiResolution r)
    {
        DetailPanel.Visibility = Visibility.Visible;

        var writeVisibility = CanWrite ? Visibility.Visible : Visibility.Collapsed;

        ActionBar.Visibility         = writeVisibility;
        DeleteBtn.Visibility         = writeVisibility;
        InlineActionRow.Visibility   = writeVisibility;
        InlineEditBtn.Visibility     = writeVisibility;
        InlineOpenFileBtn.Visibility = !string.IsNullOrEmpty(r.DocumentPath) ? Visibility.Visible : Visibility.Collapsed;

        DetailId.Text      = $"Resolution No. {r.ResolutionNumber}  ·  {r.SbTerm}";
        DetailTitle.Text   = r.Title;
        DetailSubtext.Text = $"{r.SessionInfo}  ·  Sponsor: {r.Sponsor}";

        PdfBadge.Visibility = !string.IsNullOrEmpty(r.DocumentPath) ? Visibility.Visible : Visibility.Collapsed;

        MetadataGrid.Children.Clear();
        AddMetaCell(r.Committee ?? "—", "Committee");
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