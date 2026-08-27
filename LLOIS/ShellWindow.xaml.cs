namespace LLOIS;

using System.Windows;
using LLOIS.Data;
using LLOIS.Models;
using LLOIS.Services;
using LLOIS.Views;

public partial class ShellWindow : Window
{
    private bool _isRedirecting;

    public ShellWindow()
    {
        InitializeComponent();
        ConnectionFailureHandler.ConnectionLost += OnConnectionLost;
        ShowLogin();
    }

    private void OnConnectionLost()
    {
        if (_isRedirecting) return;
        RedirectToLogin("Network connection lost. Please log in again.");
    }

    public void RedirectToLogin(string? message = null)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => RedirectToLogin(message));
            return;
        }

        if (_isRedirecting) return;
        _isRedirecting = true;

        try
        {
            if (!string.IsNullOrEmpty(message))
            {
                MessageBox.Show(message, "Connection Lost", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            Title = "DLIS - Damulog Legislative Information System";
            ShowLogin();
        }
        finally
        {
            _isRedirecting = false;
        }
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
        Title = $"DLIS — {user.Username} ({user.Role})";
        ViewHost.Content = mainView;
        mainView.PreloadData();
    }

    private void OnLogoutRequested()
    {
        Title = "DLIS - Damulog Legislative Information System";
        ShowLogin();
    }
}