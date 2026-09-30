namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using LLOIS.Services;

public partial class UserManagementView : UserControl
{
    private readonly IAuthService _auth;
    private bool _loaded;
    private int  _loadId;   // guards against out-of-order responses on quick refreshes

    public UserManagementView(IAuthService auth)
    {
        InitializeComponent();
        _auth = auth;
    }

    public void ReloadIfNeeded()
    {
        if (_loaded) return;
        _loaded = true;
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var loadId = ++_loadId;
        try
        {
            var users = (await _auth.GetAllUsersAsync()).ToList();
            if (loadId != _loadId) return;   // a newer request superseded this one

            UsersGrid.ItemsSource = users;
        }
        catch (Exception ex)
        {
            _loaded = false;   // let the next visit retry instead of staying empty

            if (ConnectionFailureHandler.HandleIfApiFailure(ex))
                return;

            MessageBox.Show($"Error loading users:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Runs one API call with the clicked button disabled (no double-submits) and the
    /// standard error handling. Returns true if the call succeeded.
    /// </summary>
    private static async Task<bool> TryAsync(object sender, Func<Task> action)
    {
        var button = sender as Button;
        if (button is not null) button.IsEnabled = false;

        try
        {
            await action();
            return true;
        }
        catch (Exception ex)
        {
            if (!ConnectionFailureHandler.HandleIfApiFailure(ex))
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        finally
        {
            if (button is not null) button.IsEnabled = true;
        }
    }

    private async void AddUserBtn_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AddUserDialog { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() != true) return;

        var ok = await TryAsync(sender,
            () => _auth.CreateUserAsync(dlg.NewUsername, dlg.NewPassword, (int)dlg.NewRole));
        if (!ok) return;

        await RefreshAsync();
        MessageBox.Show($"User '{dlg.NewUsername}' created successfully.", "Success",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void EditUserBtn_Click(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not ApiUserSummary user) return;

        var dlg = new EditUserDialog(user) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() != true) return;

        var ok = await TryAsync(sender,
            () => _auth.UpdateUserAsync(user.Id, dlg.UpdatedUsername, (int)dlg.UpdatedRole));
        if (!ok) return;

        await RefreshAsync();
        MessageBox.Show($"User '{dlg.UpdatedUsername}' updated successfully.", "Success",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void ResetPasswordBtn_Click(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not ApiUserSummary user) return;

        var dlg = new ResetPasswordDialog(user.Username) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() != true) return;

        var ok = await TryAsync(sender, () => _auth.ResetPasswordAsync(user.Id, dlg.NewPassword));
        if (!ok) return;

        MessageBox.Show($"Password for '{user.Username}' has been reset.", "Success",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void DeactivateBtn_Click(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not ApiUserSummary user) return;
        if (!user.IsActive) { MessageBox.Show("User is already inactive."); return; }

        var result = MessageBox.Show($"Deactivate user '{user.Username}'?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        if (await TryAsync(sender, () => _auth.SetActiveStatusAsync(user.Id, false)))
            await RefreshAsync();
    }

    private async void ReactivateBtn_Click(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not ApiUserSummary user) return;
        if (user.IsActive) { MessageBox.Show("User is already active."); return; }

        if (await TryAsync(sender, () => _auth.SetActiveStatusAsync(user.Id, true)))
            await RefreshAsync();
    }
}