namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using LLOIS.Models;

public partial class EditUserDialog : Window
{
    public int UserId { get; }
    public string UpdatedUsername { get; private set; } = "";
    public UserRole UpdatedRole { get; private set; } = UserRole.Viewer;

    public EditUserDialog(User user)
    {
        InitializeComponent();

        UserId = user.Id;
        UsernameBox.Text = user.Username;
        RoleCombo.SelectedIndex = user.Role switch
        {
            UserRole.Admin   => 2,
            UserRole.Encoder => 1,
            _                => 0
        };
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrText.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(UsernameBox.Text))
        { ShowErr("Username is required."); return; }

        UpdatedUsername = UsernameBox.Text.Trim();
        UpdatedRole = (RoleCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
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
