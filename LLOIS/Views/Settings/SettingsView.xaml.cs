namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using LLOIS.Services;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();

        VersionLabel.Text = $"DLIS version {App.CurrentVersion}";

        _ = CheckForUpdateSilentlyAsync();
    }

    public void RefreshUpdateCheck() => _ = CheckForUpdateSilentlyAsync();

    private async Task CheckForUpdateSilentlyAsync()
    {
        try
        {
            var update = await UpdateService.CheckForUpdateAsync();
            if (update is not null)
            {
                UpdateAvailableBadge.Visibility = Visibility.Visible;
                UpdateStatusText.Text = $"Version {update.Version} available";
            }
        }
        catch
        {
            // Silent — don't bother the user if the check fails on page load
        }
    }

    // ── Check for Updates ─────────────────────────────────────────────────

    private async void CheckUpdateBtn_Click(object sender, RoutedEventArgs e)
    {
        UpdateAvailableBadge.Visibility = Visibility.Collapsed;
        CheckUpdateBtn.IsEnabled = false;
        UpdateStatusText.Text = "Checking for updates...";

        try
        {
            var update = await UpdateService.CheckForUpdateAsync();
            if (update is null)
            {
                UpdateStatusText.Text = $"You're on the latest version ({App.CurrentVersion}).";
                CheckUpdateBtn.IsEnabled = true;
                return;
            }

            UpdateStatusText.Text = $"Version {update.Version} is available.";

            var result = MessageBox.Show(
                $"A new version ({update.Version}) is available. Update now?\n\nThe app will restart.",
                "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result != MessageBoxResult.Yes)
            {
                CheckUpdateBtn.IsEnabled = true;
                return;
            }

            var progressWindow = new UpdateProgressWindow { Owner = Window.GetWindow(this) };
            progressWindow.Show();

            var progress = new Progress<double>(percent => progressWindow.SetProgress(percent));
            var path = await UpdateService.DownloadUpdateAsync(update, progress);

            progressWindow.SetStatus("Restarting...");
            await Task.Delay(500);

            UpdateService.ApplyUpdateAndRestart(path);
        }
        catch (Exception ex)
        {
            CheckUpdateBtn.IsEnabled = true;
            UpdateStatusText.Text = "Update check failed.";

            if (!ConnectionFailureHandler.HandleIfApiFailure(ex))
                MessageBox.Show($"Update check failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}