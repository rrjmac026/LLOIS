namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using LLOIS.Models;
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
        var logs = _auth.GetAuditLog().OrderByDescending(l => l.Timestamp).ToList();
        AuditGrid.ItemsSource = logs;
        RecordCount.Text = $"{logs.Count} record(s)";
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => this.Close();
}