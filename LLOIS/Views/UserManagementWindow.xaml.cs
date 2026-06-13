namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using LLOIS.Models;
using LLOIS.Services;

public partial class UserManagementWindow : Window
{
    private readonly IAuthService _auth;

    public UserManagementWindow(IAuthService auth)
    {
        InitializeComponent();
        _auth = auth;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) => Refresh();

    private void Refresh() => UsersGrid.ItemsSource = _auth.GetAllUsers().ToList();

    private void AddUserBtn_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AddUserDialog { Owner = this };
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
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ResetPasswordBtn_Click(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not User user) return;

        var dlg = new ResetPasswordDialog(user.Username) { Owner = this };
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
            _auth.SetActiveStatus(user.Id, false);
            Refresh();
        }
    }

    private void ReactivateBtn_Click(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not User user) return;
        if (user.IsActive) { MessageBox.Show("User is already active."); return; }

        _auth.SetActiveStatus(user.Id, true);
        Refresh();
    }
}
