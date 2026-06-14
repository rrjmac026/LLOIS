using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Views;

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
    private readonly AppDbContext _db;

    public event Action<User, AppDbContext>? LoginSucceeded;

    public LoginView()
    {
        InitializeComponent();
        _db = new AppDbContext();
        DbSeeder.Seed(_db);
        _auth = new AuthService(new UserRepository(_db), _db);
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

        ErrorBanner.Visibility = Visibility.Collapsed;
        LoginSucceeded?.Invoke(user, _db);
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e) => TryLogin();

    private void Field_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) TryLogin();
    }
}