namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using LLOIS.Models;
using LLOIS.Services;

public partial class UserManagementView : UserControl
{
    private readonly IAuthService _auth;
    private bool _loaded;

    public UserManagementView(IAuthService auth)
    {
        InitializeComponent();
        _auth = auth;
    }

    public void ReloadIfNeeded()
    {
        if (_loaded) return;
        _loaded = true;
        Refresh();
    }

    private void Refresh()
    {
        try
        {
            UsersGrid.ItemsSource = _auth.GetAllUsers().ToList();
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                return;

            MessageBox.Show($"Error loading users:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddUserBtn_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AddUserDialog { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                _auth.CreateUser(dlg.NewUsername, dlg.NewPassword, dlg.NewRole);
                Refresh();
                MessageBox.Show($"User '{dlg.NewUsername}' created successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                if (ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                    return;

                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void EditUserBtn_Click(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not User user) return;

        var dlg = new EditUserDialog(user) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                _auth.UpdateUser(user.Id, dlg.UpdatedUsername, dlg.UpdatedRole);
                Refresh();
                MessageBox.Show($"User '{dlg.UpdatedUsername}' updated successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                if (ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                    return;

                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ResetPasswordBtn_Click(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not User user) return;

        var dlg = new ResetPasswordDialog(user.Username) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                _auth.ResetPassword(user.Id, dlg.NewPassword);
                MessageBox.Show($"Password for '{user.Username}' has been reset.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                if (ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                    return;

                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void DeactivateBtn_Click(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not User user) return;
        if (!user.IsActive) { MessageBox.Show("User is already inactive."); return; }

        var result = MessageBox.Show($"Deactivate user '{user.Username}'?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                _auth.SetActiveStatus(user.Id, false);
                Refresh();
            }
            catch (Exception ex)
            {
                if (ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                    return;

                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ReactivateBtn_Click(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not User user) return;
        if (user.IsActive) { MessageBox.Show("User is already active."); return; }

        try
        {
            _auth.SetActiveStatus(user.Id, true);
            Refresh();
        }
        catch (Exception ex)
        {
            if (ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(ex))
                return;

            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
