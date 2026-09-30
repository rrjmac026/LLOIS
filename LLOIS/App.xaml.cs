namespace LLOIS;

using System;
using System.Threading.Tasks;
using System.Windows;
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
        if (ConnectionFailureHandler.HandleIfApiFailure(e.Exception))
        {
            e.Handled = true;
            return;
        }

        MessageBox.Show($"Unhandled exception:\n{e.Exception}", "Debug — Unhandled Exception",
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (ConnectionFailureHandler.HandleIfApiFailure(e.Exception))
            e.SetObserved();
    }

    private void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            if (!ConnectionFailureHandler.HandleIfApiFailure(exception))
            {
                MessageBox.Show($"Unhandled domain exception:\n{exception}", "Debug — Domain Exception",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    

    private void TryStartup()
    {
        try
        {
            ThemeService.Apply(dark: false);
            var shell = new ShellWindow();
            shell.Show();
        }
        catch (Exception ex)
        {
            var result = MessageBox.Show(
                $"DLIS failed to start:\n{ex.Message}\n\nTry again?",
                "Startup Error", MessageBoxButton.RetryCancel, MessageBoxImage.Error);

            if (result == MessageBoxResult.Retry)
                TryStartup();
            else
                Shutdown();
        }
    }
}