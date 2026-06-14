using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Views;

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LLOIS.Data;
using LLOIS.Models;
using LLOIS.Repositories;
using LLOIS.Services;

public partial class LoginView : UserControl
{
    private readonly IAuthService _auth;
    private readonly SimpleDbContextFactory _factory;

    public event Action<User, SimpleDbContextFactory>? LoginSucceeded;

    public LoginView()
    {
        InitializeComponent();
        _factory = new SimpleDbContextFactory();
        Task.Run(() =>
        {
            using var db = _factory.CreateDbContext();
            DbSeeder.Seed(db);
        });
        _auth = new AuthService(new UserRepository(_factory), _factory);
    }

    private async void TryLogin()
    {
        string username = UsernameBox.Text.Trim();
        string password = PasswordBox.Password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ErrorText.Text = "Please fill in all fields.";
            ErrorBanner.Visibility = Visibility.Visible;
            return;
        }

        LoginButton.IsEnabled = false;
        ErrorBanner.Visibility = Visibility.Collapsed;

        var user = await Task.Run(() => _auth.Login(username, password));

        LoginButton.IsEnabled = true;

        if (user is null)
        {
            ErrorText.Text = "Invalid username or password.";
            ErrorBanner.Visibility = Visibility.Visible;
            return;
        }

        LoginSucceeded?.Invoke(user, _factory);
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e) => TryLogin();

    private void Field_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) TryLogin();
    }
}