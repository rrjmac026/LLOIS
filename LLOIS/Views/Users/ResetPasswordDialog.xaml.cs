namespace LLOIS.Views;

using System.Windows;

public partial class ResetPasswordDialog : Window
{
    public string NewPassword { get; private set; } = "";

    public ResetPasswordDialog(string username)
    {
        InitializeComponent();
        HeaderText.Text = $"Reset password for: {username}";
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        ErrText.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(PasswordBox.Password))
        { ShowErr("Password is required."); return; }
        if (PasswordBox.Password != ConfirmBox.Password)
        { ShowErr("Passwords do not match."); return; }
        if (PasswordBox.Password.Length < 6)
        { ShowErr("Minimum 6 characters required."); return; }

        NewPassword = PasswordBox.Password;
        DialogResult = true;
    }

    private void ShowErr(string msg)
    {
        ErrText.Text = msg;
        ErrText.Visibility = Visibility.Visible;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
