namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LLOIS.Services;

public partial class AuditLogView : UserControl
{
    private readonly IAuthService _auth;
    private readonly DispatcherTimer _timer;
    private bool _loaded;
    private bool _refreshing;
    private int? _lastTopId;
    private int _lastCount;

    public AuditLogView(IAuthService auth)
    {
        InitializeComponent();
        _auth = auth;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _timer.Tick += async (_, _) => await RefreshAsync(silent: true);

        // Poll only while this page is actually on screen.
        IsVisibleChanged += OnVisibleChanged;
        Unloaded += (_, _) => _timer.Stop();
    }

    private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            _timer.Start();
            if (_loaded) Refresh(); // catch up immediately when you come back to the page
        }
        else
        {
            _timer.Stop();
        }
    }

    public void ReloadIfNeeded()
    {
        if (_loaded) return;
        _loaded = true;
        Refresh();
    }

    public void Refresh() => _ = RefreshAsync();

    private async Task RefreshAsync(bool silent = false)
    {
        if (_refreshing) return; // don't stack requests if the server is slow
        _refreshing = true;

        try
        {
            var logs = (await _auth.GetRecentLogsAsync()).ToList();

            // Audit logs only grow, so if the newest id and count are unchanged, skip the redraw.
            var topId = logs.FirstOrDefault()?.Id;
            if (silent && topId == _lastTopId && logs.Count == _lastCount)
                return;

            _lastTopId = topId;
            _lastCount = logs.Count;

            // Keep the user's selection across refreshes.
            var selectedId = (AuditGrid.SelectedItem as ApiAuditLog)?.Id;

            AuditGrid.ItemsSource = logs;
            RecordCount.Text = $"{logs.Count} record(s)";

            if (selectedId is not null)
                AuditGrid.SelectedItem = logs.FirstOrDefault(l => l.Id == selectedId);
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.HandleIfApiFailure(ex))
                return;

            // Background polls stay quiet; only manual refreshes show an error popup.
            if (!silent)
                MessageBox.Show($"Error loading audit logs:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void RefreshBtn_Click(object sender, RoutedEventArgs e) => Refresh();
}