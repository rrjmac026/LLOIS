using System;
using System.Collections.Generic;
using System.Text;

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
    public User? LoggedInUser { get; private set; }

    public LoginWindow()
    {
        InitializeComponent();
        var db = new AppDbContext();
        DbSeeder.Seed(db);
        _auth = new AuthService(new UserRepository(db), db);
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