namespace LLOIS.Views;

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LLOIS.Services;

//Main View Updates file
public partial class MainView
{
    private System.Windows.Threading.DispatcherTimer? _updateCheckTimer;

    private void StartUpdateChecks()
    {
        CheckForUpdateAndUpdateBadge();

        _updateCheckTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(15)
        };
        _updateCheckTimer.Tick += (s, args) => CheckForUpdateAndUpdateBadge();
        _updateCheckTimer.Start();
    }

    private void CheckForUpdateAndUpdateBadge()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var update = await UpdateService.CheckForUpdateAsync();
                Dispatcher.Invoke(() =>
                {
                    SettingsBtnRef.ApplyTemplate();
                    var settingsBadge = SettingsBtnRef.Template.FindName("UpdateBadge", SettingsBtnRef) as Border;
                    if (settingsBadge is not null)
                        settingsBadge.Visibility = update is not null ? Visibility.Visible : Visibility.Collapsed;
                });
            }
            catch
            {
                // Silent — offline is fine, just skip this check cycle
            }
        });
    }

    /// <summary>One-time confirmation shown if we just relaunched from an update.</summary>
    private void ShowUpdateCompleteNotice()
    {
        var previousVersion = UpdateService.ConsumeUpdateMarker();
        if (previousVersion is null) return;

        MessageBox.Show(
            $"DLIS was successfully updated to version {App.CurrentVersion}.",
            "Update Complete",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private async void CheckUpdateBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var update = await UpdateService.CheckForUpdateAsync();
            if (update is null)
            {
                MessageBox.Show($"You're on the latest version ({App.CurrentVersion}).",
                    "No Updates", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"A new version ({update.Version}) is available. Update now?\n\nThe app will restart.",
                "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result != MessageBoxResult.Yes) return;

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
            if (!ConnectionFailureHandler.HandleIfApiFailure(ex))
                MessageBox.Show($"Update check failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}