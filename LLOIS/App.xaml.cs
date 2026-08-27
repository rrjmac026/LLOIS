namespace LLOIS;

using System;
using System.Threading.Tasks;
using System.Windows;
using LLOIS.Data;
using LLOIS.Services;
using LLOIS.Views;

public partial class App : Application
{
    public const string CurrentVersion = "1.3.6";

    protected override void OnStartup(StartupEventArgs e)
    {
        PdfFontResolver.Apply();
        RegisterGlobalExceptionHandlers();
        base.OnStartup(e);

        TryStartup();
    }
    

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        if (ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(e.Exception))
        {
            e.Handled = true;
            return;
        }

        // TEMP: show the real exception so we can see what's actually crashing
        MessageBox.Show($"Unhandled exception:\n{e.Exception}", "Debug — Unhandled Exception",
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true; // prevent full crash while debugging
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(e.Exception))
            e.SetObserved();
    }

        private void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            if (!ConnectionFailureHandler.RedirectToLoginIfConnectionFailure(exception))
            {
                MessageBox.Show($"Unhandled domain exception:\n{exception}", "Debug — Domain Exception",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Optional: detects the Windows 10/11 dark-mode registry setting.
    /// </summary>
    private static bool IsSystemDark()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser
                .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var val = key?.GetValue("AppsUseLightTheme");
            return val is int i && i == 0;
        }
        catch { return false; }
    }

    private void TryStartup()
    {
        try
        {
            using var db = new AppDbContext();
            DbSeeder.Seed(db);

            ThemeService.Apply(dark: false);
            var shell = new ShellWindow();
            shell.Show();
        }
        catch (Exception ex)
        {
            var result = MessageBox.Show(
                ConnectionFailureHandler.IsConnectionFailure(ex)
                    ? "No internet connection. DLIS needs an internet connection to start.\n\nTry again?"
                    : $"DLIS failed to start:\n{ex.Message}\n\nTry again?",
                "Startup Error", MessageBoxButton.RetryCancel, MessageBoxImage.Error);

            if (result == MessageBoxResult.Retry)
                TryStartup();
            else
                Shutdown();
        }
    }

    
}