namespace LLOIS.Views;

using System.Windows;
using LLOIS.Services;

public partial class AuditLogWindow : Window
{
    private readonly IAuthService _auth;

    public AuditLogWindow(IAuthService auth)
    {
        InitializeComponent();
        _auth = auth;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var logs = _auth.GetRecentLogs(200).ToList();
        LogGrid.ItemsSource = logs;
        CountLabel.Text = $"{logs.Count} entries";
    }
}
