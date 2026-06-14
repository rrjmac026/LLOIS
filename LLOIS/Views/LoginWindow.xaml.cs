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
        string username = UsernameBox.Text.Trim();
        string password = PasswordBox.Password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ErrorText.Text = "Please fill in all fields.";
            ErrorBanner.Visibility = Visibility.Visible;
            return;
        }

        var user = _auth.Login(username, password);

        if (user is null)
        {
            ErrorText.Text = "Invalid username or password.";
            ErrorBanner.Visibility = Visibility.Visible;
            return;
        }

        LoggedInUser = user;
        ErrorBanner.Visibility = Visibility.Collapsed;
        DialogResult = true;
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e) => TryLogin();

    private void Field_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) TryLogin();
    }
}