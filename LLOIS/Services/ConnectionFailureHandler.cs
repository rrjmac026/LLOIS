namespace LLOIS.Services;

using System;
using System.Data.Common;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Npgsql;
using LLOIS;

public static class ConnectionFailureHandler
{
    public static event Action? ConnectionLost;

    public static void RaiseConnectionLost() => ConnectionLost?.Invoke();

    private static readonly string[] ConnectionFailureMessages =
    {
        "failed to open",
        "unable to connect",
        "connection was lost",
        "connection reset",
        "could not open connection",
        "could not connect",
        "connection string",
        "server was not found",
        "host not found",
        "no such host",
        "connection is broken",
        "operation timed out",
        "network is unreachable",
        "network unreachable",
        "no route to host"
    };

    // Checked BEFORE the generic connection keywords, since messages like
    // "too many connections" or "disk full" would otherwise be misclassified
    // as a plain lost-connection error.
    private static readonly string[] DatabaseFullMessages =
    {
        "database is full",
        "out of disk",
        "disk full",
        "no space left",
        "quota exceeded",
        "storage limit",
        "storage quota",
        "disk quota",
        "too many connections",
        "too many clients",
        "remaining connection slots",
        "connection slots are reserved",
        "sorry, too many clients"
    };

    public static bool IsDatabaseFullFailure(Exception? exception)
    {
        if (exception is null) return false;

        if (exception is AggregateException aggregate)
            return aggregate.InnerExceptions.Any(IsDatabaseFullFailure);

        if (exception is TargetInvocationException tie && tie.InnerException is not null)
            return IsDatabaseFullFailure(tie.InnerException);

        if (IsDatabaseFullMessage(exception.Message))
            return true;

        return IsDatabaseFullFailure(exception.InnerException);
    }

    private static bool IsDatabaseFullMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        var normalized = message.ToLowerInvariant();
        return DatabaseFullMessages.Any(keyword => normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsConnectionFailure(Exception? exception)
    {
        if (exception is null) return false;

        if (exception is AggregateException aggregate)
            return aggregate.InnerExceptions.Any(IsConnectionFailure);

        if (exception is TargetInvocationException tie && tie.InnerException is not null)
            return IsConnectionFailure(tie.InnerException);

        if (exception is SocketException) return true;
        if (exception is TimeoutException) return true;
        if (exception is NpgsqlException) return true;
        if (exception is System.Net.Http.HttpRequestException) return true;
        if (exception is TaskCanceledException) return true;
        if (exception is DbException dbException)
            return IsConnectionFailureMessage(dbException.Message)
                || IsConnectionFailure(dbException.InnerException);

        if (exception is InvalidOperationException && IsConnectionFailureMessage(exception.Message))
            return true;

        if (exception is InvalidOperationException && exception.InnerException is not null)
            return IsConnectionFailure(exception.InnerException);

        return IsConnectionFailure(exception.InnerException);
    }

    private static bool IsConnectionFailureMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;

        var normalized = message.ToLowerInvariant();
        return ConnectionFailureMessages.Any(keyword => normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    public static bool RedirectToLoginIfConnectionFailure(Exception exception)
    {
        // Database-full is checked first: it's a more specific diagnosis than
        // a generic lost connection, and some of its keywords (e.g. "too many
        // connections") would otherwise match the generic connection list.
        if (IsDatabaseFullFailure(exception))
        {
            LogError(exception);
            ShowGlobalMessage("Database is full. Please contact the system administrator.");
            return true;
        }

        if (!IsConnectionFailure(exception)) return false;

        LogError(exception);

        ShowGlobalMessage("Network connection lost. Please log in again.");
        return true;
    }

    private static void ShowGlobalMessage(string message)
    {
        var app = Application.Current;
        if (app is not null)
        {
            app.Dispatcher.BeginInvoke(() =>
            {
                if (app.MainWindow is ShellWindow shell)
                {
                    shell.RedirectToLogin(message);
                }
                else
                {
                    ConnectionLost?.Invoke();
                }
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