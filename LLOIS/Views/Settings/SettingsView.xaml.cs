namespace LLOIS.Views;

using System.IO;
using System.Windows;
using System.Windows.Controls;
using LLOIS.Data;
using LLOIS.Models;
using LLOIS.Services;

public partial class SettingsView : UserControl
{
    private readonly User                   _currentUser;
    private readonly IAuthService           _auth;
    private readonly SimpleDbContextFactory _dbFactory;

    public void RefreshUpdateCheck() => _ = CheckForUpdateSilentlyAsync();

    public SettingsView(User currentUser, IAuthService auth, SimpleDbContextFactory dbFactory)
    {
        InitializeComponent();
        _currentUser = currentUser;
        _auth        = auth;
        _dbFactory   = dbFactory;

        VersionLabel.Text = $"DLIS version {App.CurrentVersion}";

        bool isAdmin = currentUser.Role == UserRole.Admin;
        BackupBtn.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

        _ = CheckForUpdateSilentlyAsync();
    }

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

            if (!ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                MessageBox.Show($"Update check failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Backup Data ────────────────────────────────────────────────────────

    private async void BackupBtn_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "ZIP Archive (*.zip)|*.zip",
            FileName = $"DLIS_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip"
        };
        if (dlg.ShowDialog() != true) return;

        BackupBtn.IsEnabled = false;

        var progressWindow = new UpdateProgressWindow { Owner = Window.GetWindow(this) };
        progressWindow.Title = "Backing Up DLIS";
        progressWindow.SetIndeterminate();
        progressWindow.Show();

        var progress = new Progress<string>(status =>
        {
            BackupStatusText.Text = status;
            progressWindow.SetStatus(status);
        });

        try
        {
            await BackupService.CreateBackupAsync(_dbFactory, dlg.FileName, progress);

            _ = Task.Run(() =>
            {
                try { _auth.LogAction(_currentUser, "BACKUP", $"Created data backup: {Path.GetFileName(dlg.FileName)}"); }
                catch { /* non-critical */ }
            });

            BackupStatusText.Text = "Backup complete.";
            progressWindow.Close();

            MessageBox.Show($"Backup saved to:\n{dlg.FileName}", "Backup Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            progressWindow.Close();
            BackupStatusText.Text = "Backup failed.";

            if (!ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                MessageBox.Show($"Backup failed:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BackupBtn.IsEnabled = true;
        }
    }
}