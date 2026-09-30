namespace LLOIS;

using System.Windows;
using LLOIS.Services;
using LLOIS.Views;

public partial class ShellWindow : Window
{
    private bool _isRedirecting;
    private readonly ApiClient _api;

    public ShellWindow()
    {
        InitializeComponent();
        _api = new ApiClient("https://dlis-web.onrender.com/"); // TODO: move to config/appsettings
        ConnectionFailureHandler.ConnectionLost += OnConnectionLost;
        ConnectionFailureHandler.SessionExpired += OnConnectionLost;
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
            _api.ClearToken();

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
        var loginView = new LoginView(_api);
        loginView.LoginSucceeded += OnLoginSucceeded;
        ViewHost.Content = loginView;
    }

    private void OnLoginSucceeded(ApiUser user, ApiClient api)
    {
        var mainView = new MainView(user, api);
        mainView.LogoutRequested += OnLogoutRequested;
        Title = $"DLIS — {user.Username} ({user.RoleName})";
        ViewHost.Content = mainView;
        mainView.PreloadData();
    }

    private void OnLogoutRequested()
    {
        Title = "DLIS - Damulog Legislative Information System";
        ShowLogin();
    }
}