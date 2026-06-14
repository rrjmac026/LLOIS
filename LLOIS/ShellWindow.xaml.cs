using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS;

using System.Windows;
using System.Windows.Controls;
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

    private void OnLoginSucceeded(User user, AppDbContext db)
    {
        _db = db;
        var mainView = new MainView(user, db);
        mainView.LogoutRequested += OnLogoutRequested;
        FadeTo(mainView);

        // Update title bar
        Title = $"LLOIS — {user.Username} ({user.Role})";
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
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(180));
        var fadeIn  = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220));

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