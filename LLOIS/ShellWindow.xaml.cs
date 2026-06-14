namespace LLOIS;

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using LLOIS.Data;
using LLOIS.Models;
using LLOIS.Views;

public partial class ShellWindow : Window
{
    private AppDbContext? _db;

    public ShellWindow()
    {
        InitializeComponent();
        ShowLogin();
    }

    private void ShowLogin()
    {
        var loginView = new LoginView();
        loginView.LoginSucceeded += OnLoginSucceeded;
        FadeTo(loginView);
    }

    private void OnLoginSucceeded(User user, SimpleDbContextFactory factory)
    {
        var mainView = new MainView(user, factory);
        mainView.LogoutRequested += OnLogoutRequested;
        Title = $"LLOIS — {user.Username} ({user.Role})";
        FadeTo(mainView);
        mainView.PreloadData();
    }

    private async void OnLogoutRequested()
    {
        // Start the fade immediately, dispose db after
        var db = _db;
        _db = null;
        Title = "LLOIS - Local Legislative Ordinance Information System";

        // Build new LoginView before fade so it's ready
        var loginView = new LoginView();
        loginView.LoginSucceeded += OnLoginSucceeded;

        FadeTo(loginView);

        // Dispose old db after transition completes
        await Task.Delay(300);
        db?.Dispose();
    }

    private void FadeTo(UIElement newView)
    {
        ViewHost.Content = newView;
    }
}