namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using LLOIS.Services;

public partial class AuditLogView : UserControl
{
    private readonly IAuthService _auth;
    private bool _loaded;

    public AuditLogView(IAuthService auth)
    {
        InitializeComponent();
        _auth = auth;
    }

    /// <summary>
    /// Loads data the first time the page is shown. Call again (or just
    /// call Refresh directly) if you want it to re-pull every visit.
    /// </summary>
    public void ReloadIfNeeded()
    {
        if (_loaded) return;
        _loaded = true;
        Refresh();
    }

    public void Refresh()
    {
        var logs = _auth.GetRecentLogs().OrderByDescending(l => l.Timestamp).ToList();
        AuditGrid.ItemsSource = logs;
        RecordCount.Text = $"{logs.Count} record(s)";
    }

    private void RefreshBtn_Click(object sender, RoutedEventArgs e) => Refresh();
}
