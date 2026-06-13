namespace LLOIS.Views;

using System.Windows;
using System.Windows.Input;
using LLOIS.Data;
using LLOIS.Models;
using LLOIS.Repositories;
using LLOIS.Services;

public partial class LoginWindow : Window
{
    private readonly IAuthService _auth;
    public readonly AppDbContext Db;
    public User? LoggedInUser { get; private set; }

    public LoginWindow()
    {
        InitializeComponent();
        Db = new AppDbContext();
        DbSeeder.Seed(Db);
        _auth = new AuthService(new UserRepository(Db), Db);
    }

    private void TryLogin()
    {
        var user = _auth.Login(UsernameBox.Text.Trim(), PasswordBox.Password);

        if (user is null)
        {
            ErrorText.Text = "Invalid username or password.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        LoggedInUser = user;
        DialogResult = true;
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e) => TryLogin();
    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) TryLogin();
    }
}