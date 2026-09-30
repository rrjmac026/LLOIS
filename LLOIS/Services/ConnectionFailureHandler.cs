namespace LLOIS.Services;

using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using LLOIS;

public static class ConnectionFailureHandler
{
    public static event Action? ConnectionLost;
    public static event Action? SessionExpired;

    public static void RaiseConnectionLost() => ConnectionLost?.Invoke();

    private static readonly string[] NetworkFailureMessages =
    {
        "failed to open",
        "unable to connect",
        "connection was lost",
        "connection reset",
        "could not connect",
        "server was not found",
        "host not found",
        "no such host",
        "network is unreachable",
        "network unreachable",
        "no route to host",
        "operation timed out",
        "the operation was canceled"
    };

    public static bool IsSessionExpired(Exception? exception)
    {
        if (exception is null) return false;

        if (exception is AggregateException aggregate)
            return aggregate.InnerExceptions.Any(IsSessionExpired);

        if (exception is TargetInvocationException tie && tie.InnerException is not null)
            return IsSessionExpired(tie.InnerException);

        return exception is UnauthorizedAccessException
            || IsSessionExpired(exception.InnerException);
    }

    public static bool IsConnectionFailure(Exception? exception)
    {
        if (exception is null) return false;

        if (exception is AggregateException aggregate)
            return aggregate.InnerExceptions.Any(IsConnectionFailure);

        if (exception is TargetInvocationException tie && tie.InnerException is not null)
            return IsConnectionFailure(tie.InnerException);

        // HttpRequestException with a StatusCode means we DID reach the server —
        // it just returned an error (422, 500, etc). That's not a connection failure.
        if (exception is HttpRequestException httpEx)
            return httpEx.StatusCode is null;

        if (exception is TaskCanceledException) return true; // covers HttpClient timeouts
        if (exception is System.Net.Sockets.SocketException) return true;

        if (IsNetworkFailureMessage(exception.Message)) return true;

        return IsConnectionFailure(exception.InnerException);
    }

    private static bool IsNetworkFailureMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        var normalized = message.ToLowerInvariant();
        return NetworkFailureMessages.Any(keyword => normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Central dispatch: call this from every catch block that might see an
    /// API failure. Returns true if it handled the exception (redirected to
    /// login or showed a message) — caller should treat it as handled.
    /// </summary>
    public static bool HandleIfApiFailure(Exception exception)
    {
        if (IsSessionExpired(exception))
        {
            LogError(exception);
            ShowGlobalMessage("Your session has expired. Please log in again.");
            SessionExpired?.Invoke();
            return true;
        }

        if (IsConnectionFailure(exception))
        {
            LogError(exception);
            ShowGlobalMessage("Network connection lost. Please check your internet connection and try again.");
            ConnectionLost?.Invoke();
            return true;
        }

        return false;
    }

    private static void ShowGlobalMessage(string message)
    {
        var app = Application.Current;
        if (app is not null)
        {
            app.Dispatcher.BeginInvoke(() =>
            {
                if (app.MainWindow is ShellWindow shell)
                    shell.RedirectToLogin(message);
                else
                    ConnectionLost?.Invoke();
            });
        }
        else
        {
            ConnectionLost?.Invoke();
        }
    }

    private static void LogError(Exception exception)
    {
        try
        {
            var logPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "LLOIS_connection_errors.log");

            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n" +
                        $"Type: {exception.GetType().FullName}\n" +
                        $"Message: {exception.Message}\n" +
                        $"Inner: {exception.InnerException?.Message ?? "none"}\n" +
                        $"StackTrace: {exception.StackTrace}\n" +
                        new string('-', 60) + "\n";

            System.IO.File.AppendAllText(logPath, entry);
        }
        catch { /* logging must never crash the app */ }
    }
}