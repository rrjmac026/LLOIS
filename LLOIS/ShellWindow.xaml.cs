namespace LLOIS;

using System.Windows;
using LLOIS.Data;
using LLOIS.Models;
using LLOIS.Views;

public partial class ShellWindow : Window
{
    public ShellWindow()
    {
        InitializeComponent();
        ShowLogin();
    }

    private void ShowLogin()
    {
        var loginView = new LoginView();
        loginView.LoginSucceeded += OnLoginSucceeded;
        ViewHost.Content = loginView;
    }

    private void OnLoginSucceeded(User user, SimpleDbContextFactory factory)
    {
        var mainView = new MainView(user, factory);
        mainView.LogoutRequested += OnLogoutRequested;
        Title = $"LLOIS — {user.Username} ({user.Role})";
        ViewHost.Content = mainView;
        mainView.PreloadData();
    }

    private void OnLogoutRequested()
    {
        Title = "LLOIS - Local Legislative Ordinance Information System";
        ShowLogin();
    }
}