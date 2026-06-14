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

    private async void OnLoginSucceeded(User user, AppDbContext db)
    {
        _db = db;
        // Load ordinances on background thread while fade plays
        var mainView = new MainView(user, db);
        mainView.LogoutRequested += OnLogoutRequested;
        Title = $"LLOIS — {user.Username} ({user.Role})";
        FadeTo(mainView);
        // Trigger data load after fade starts, not before
        await Task.Delay(50); // let the fade frame render first
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
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
        var fadeIn  = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));

        if (ViewHost.Content is UIElement current)
        {
            fadeOut.Completed += (_, _) =>
            {
                ViewHost.Content = newView;
                newView.BeginAnimation(OpacityProperty, fadeIn);
            };
            current.BeginAnimation(OpacityProperty, fadeOut);
        }
        else
        {
            ViewHost.Content = newView;
            newView.BeginAnimation(OpacityProperty, fadeIn);
        }
    }
}