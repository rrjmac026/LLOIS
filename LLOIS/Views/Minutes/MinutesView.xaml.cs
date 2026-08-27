namespace LLOIS.Views;

using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LLOIS.Models;
using LLOIS.Services;

public partial class MinutesView : UserControl
{
    private readonly IMinutesService _service;
    private readonly IAuthService    _auth;
    private readonly User            _currentUser;
    private string    _searchQuery = string.Empty;
    private Minutes?  _selectedMinutes;
    private bool      _loaded = false;

    private readonly System.Windows.Threading.DispatcherTimer _searchTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(300)
    };

    private readonly System.Windows.Threading.DispatcherTimer _refreshTimer = new()
    {
        Interval = TimeSpan.FromSeconds(15)   // adjust to taste
    };

    public MinutesView(IMinutesService service, IAuthService auth, User user)
    {
        InitializeComponent();
        _service     = service;
        _auth        = auth;
        _currentUser = user;

        bool canWrite = user.Role is UserRole.Admin or UserRole.SuperAdmin or UserRole.Encoder;
        AddBtn.Visibility = canWrite ? Visibility.Visible : Visibility.Collapsed;

        _searchTimer.Tick += SearchTimer_Tick;

        _refreshTimer.Tick += async (s, e) => await LoadMinutesAsync();
        _refreshTimer.Start();
    }

    public void ReloadIfNeeded()
    {
        if (!_loaded)
        {
            _loaded = true;
            _ = LoadMinutesAsync();
        }
    }

    // ── Data loading ───────────────────────────────────────────────────────

    private async Task LoadMinutesAsync()
    {
        try
        {
            var results = await Task.Run(() => _service.GetAll().ToList());

            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                var q = _searchQuery;
                results = results.Where(m =>
                    m.SessionType?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false).ToList();
            }

            if (SessionTypeFilter.SelectedItem is ComboBoxItem { Content: string type }
                && type != "All sessions")
            {
                results = results.Where(m => m.SessionType == type).ToList();
            }

            MinutesList.ItemsSource = results;
            ResultCount.Text = $"{results.Count} minutes found";

            // Preserve selection/detail view across background refreshes
            if (_selectedMinutes is not null)
            {
                var stillExists = results.FirstOrDefault(m => m.Id == _selectedMinutes.Id);
                if (stillExists is not null)
                {
                    _selectedMinutes = stillExists;
                    MinutesList.SelectedItem = stillExists;
                    ShowDetail(stillExists);
                    return;
                }
            }

            ClearDetail();
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                return;

            MessageBox.Show($"Error loading minutes:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Search / filter ────────────────────────────────────────────────────

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchTimer.Stop();
        _searchTimer.Start();
    }

    private async void SearchTimer_Tick(object? sender, EventArgs e)
    {
        _searchTimer.Stop();
        _searchQuery = SearchBox.Text.Trim();
        await LoadMinutesAsync();
    }

    private async void SessionTypeFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_service is null) return;
        await LoadMinutesAsync();
    }

    // ── Selection ──────────────────────────────────────────────────────────

    private void MinutesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MinutesList.SelectedItem is not Minutes m) return;

        try
        {
            var detail = _service.GetDetails(m.Id);
            if (detail is null) return;

            _selectedMinutes = detail;
            ShowDetail(detail);
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                return;

            MessageBox.Show($"Error loading details:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── CRUD ───────────────────────────────────────────────────────────────

    private void AddBtn_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AddEditMinutesWindow(_service) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            _ = Task.Run(() =>
            {
                try { _auth.LogAction(_currentUser, "ADD", "Added minutes record"); }
                catch { /* non-critical */ }
            });
            _loaded = false;
            ReloadIfNeeded();

            MessageBox.Show("Minutes record created successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void EditBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedMinutes is null) return;
        var dlg = new AddEditMinutesWindow(_service, _selectedMinutes) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            _ = Task.Run(() =>
            {
                try { _auth.LogAction(_currentUser, "EDIT", "Edited minutes record"); }
                catch { /* non-critical */ }
            });
            _loaded = false;
            ReloadIfNeeded();

            MessageBox.Show("Minutes record updated successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OpenFileBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedMinutes?.DocumentPath is null) return;

        try
        {
            Process.Start(new ProcessStartInfo(_selectedMinutes.DocumentPath)
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
        if (_selectedMinutes is null) return;
        var result = MessageBox.Show(
            "Permanently delete this minutes record?\n\nThis cannot be undone.",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            var id = _selectedMinutes.Id;
            await Task.Run(() => _service.Delete(id));
            _ = Task.Run(() =>
            {
                try { _auth.LogAction(_currentUser, "DELETE", "Deleted minutes record"); }
                catch { /* non-critical */ }
            });
            await LoadMinutesAsync();

            MessageBox.Show("Minutes record deleted successfully.", "Success",
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

    // ── ShowDetail ─────────────────────────────────────────────────────────

    private void ShowDetail(Minutes m)
    {
        DetailPanel.Visibility = Visibility.Visible;
        ActionBar.Visibility   = Visibility.Visible;

        bool canWrite = _currentUser.Role is UserRole.Admin or UserRole.Encoder;

        EditBtn.Visibility     = Visibility.Collapsed;
        OpenFileBtn.Visibility = Visibility.Collapsed;
        DeleteBtn.Visibility   = canWrite ? Visibility.Visible : Visibility.Collapsed;
        ActionBar.Visibility   = canWrite ? Visibility.Visible : Visibility.Collapsed;

        InlineActionRow.Visibility  = canWrite ? Visibility.Visible : Visibility.Collapsed;
        InlineEditBtn.Visibility    = canWrite ? Visibility.Visible : Visibility.Collapsed;
        InlineOpenFileBtn.Visibility = m.DocumentPath is not null ? Visibility.Visible : Visibility.Collapsed;

        DetailId.Text          = m.SessionType;
        DetailSessionType.Text = m.SessionType;
        DetailDate.Text        = m.Date?.ToString("MMMM dd, yyyy") ?? "No date set";

        FileBadge.Visibility = !string.IsNullOrEmpty(m.DocumentPath) ? Visibility.Visible : Visibility.Collapsed;

        MetadataGrid.Children.Clear();
        AddMetaCell(m.SessionType, "Session type");
        AddMetaCell(m.Date?.ToString("MMMM dd, yyyy") ?? "—", "Date");
    }

    private void ClearDetail()
    {
        DetailPanel.Visibility = Visibility.Collapsed;
        ActionBar.Visibility   = Visibility.Collapsed;
        _selectedMinutes       = null;
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
}