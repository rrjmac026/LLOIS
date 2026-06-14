using System;
using System.Collections.Generic;
using System.Text;

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

        // Pre-warm EF Core connection pool on background thread
        // so MainView loads instantly when it hits the DB
        await Task.Run(() =>
        {
            using var ctx = new AppDbContext();
            ctx.Database.EnsureCreated();
        });

        var mainView = new MainView(user, db);
        mainView.LogoutRequested += OnLogoutRequested;
        Title = $"LLOIS — {user.Username} ({user.Role})";
        FadeTo(mainView);
    }

    private void OnLogoutRequested()
    {
        _db?.Dispose();
        _db = null;
        Title = "LLOIS - Local Legislative Ordinance Information System";
        ShowLogin();
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