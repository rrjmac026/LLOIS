namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using LLOIS.Models;

public partial class AddUserDialog : Window
{
    public string NewUsername { get; private set; } = "";
    public string NewPassword { get; private set; } = "";
    public UserRole NewRole { get; private set; } = UserRole.Viewer;

    public AddUserDialog() => InitializeComponent();

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        ErrText.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(UsernameBox.Text))
        { ShowErr("Username is required."); return; }
        if (string.IsNullOrWhiteSpace(PasswordBox.Password))
        { ShowErr("Password is required."); return; }
        if (PasswordBox.Password != ConfirmBox.Password)
        { ShowErr("Passwords do not match."); return; }
        if (PasswordBox.Password.Length < 6)
        { ShowErr("Password must be at least 6 characters."); return; }

        NewUsername = UsernameBox.Text.Trim();
        NewPassword = PasswordBox.Password;
        NewRole = (RoleCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
        {
            "Admin"   => UserRole.Admin,
            "Encoder" => UserRole.Encoder,
            _         => UserRole.Viewer
        };

        DialogResult = true;
    }

    private void ShowErr(string msg)
    {
        ErrText.Text = msg;
        ErrText.Visibility = Visibility.Visible;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
