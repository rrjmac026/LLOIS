namespace LLOIS;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LLOIS.Data;
using LLOIS.Models;
using LLOIS.Repositories;
using LLOIS.Services;

public partial class MainWindow : Window
{
    private readonly IOrdinanceService _service;
    private readonly IAuthService _auth;
    private readonly AppDbContext _db;
    private readonly User _currentUser;
    private string _searchQuery = string.Empty;

    public MainWindow(User user)
    {
        InitializeComponent();
        _currentUser = user;
        _db = new AppDbContext();
        _service = new OrdinanceService(new OrdinanceRepository(_db));
        _auth = new AuthService(new UserRepository(_db), _db);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Title = $"LLOIS — {_currentUser.Username} ({_currentUser.Role})";
        LoadOrdinances();
    }

    private void LoadOrdinances()
    {
        var results = _service.Search(_searchQuery).ToList();

        if (StatusFilter.SelectedItem is ComboBoxItem { Content: string status } && status != "All"
            && Enum.TryParse<OrdinanceStatus>(status.Replace(" ", ""), out var parsed))
        {
            results = results.Where(o => o.Status == parsed).ToList();
        }

        OrdinanceList.ItemsSource = results;
        ResultCount.Text = $"{results.Count} ordinance(s) found";
        DetailPanel.Visibility = Visibility.Collapsed;
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        _searchQuery = SearchBox.Text.Trim();
        if (_searchQuery == "Search by ID, subject, or series number...") _searchQuery = "";
        LoadOrdinances();
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) SearchButton_Click(sender, e);
    }

    private void StatusFilter_Changed(object sender, SelectionChangedEventArgs e) => LoadOrdinances();

    private void OrdinanceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OrdinanceList.SelectedItem is not Ordinance ordinance) return;

        var detail = _service.GetDetails(ordinance.OrdinanceNumber);
        if (detail is null) return;

        ShowDetail(detail);
    }

    private void ShowDetail(Ordinance o)
    {
        DetailPanel.Visibility = Visibility.Visible;

        DetailId.Text = o.OrdinanceNumber;
        DetailSubject.Text = o.Subject;
        DetailSeries.Text = o.SeriesNumber;
        DetailStatus.Text = o.Status.ToString();

        (StatusBadgeControl.Background, StatusBadgeControl.BorderBrush) = o.Status switch
        {
            OrdinanceStatus.InEffect   => (Brushes.LightGreen, Brushes.Green),
            OrdinanceStatus.Amended    => (Brushes.LightYellow, Brushes.Orange),
            OrdinanceStatus.Superseded => (Brushes.LightBlue, Brushes.SteelBlue),
            OrdinanceStatus.Repealed   => (Brushes.LightCoral, Brushes.Red),
            _ => (Brushes.LightGray, Brushes.Gray)
        };
        StatusBadgeControl.BorderThickness = new Thickness(1);

        LatestVersionPanel.Children.Clear();
        if (o.LatestVersion is OrdinanceVersion latest)
        {
            LatestVersionPanel.Children.Add(MakeRow("Title", latest.Title, bold: true));
            LatestVersionPanel.Children.Add(MakeRow("Date Enacted", latest.DateEnacted.ToString("MMMM dd, yyyy")));
            LatestVersionPanel.Children.Add(MakeRow("Enacted By", latest.EnactedBy));
            LatestVersionPanel.Children.Add(MakeRow("Content", latest.Content));
            if (!string.IsNullOrEmpty(latest.AmendmentNotes))
                LatestVersionPanel.Children.Add(MakeRow("Notes", latest.AmendmentNotes, color: "#CC6600"));
        }

        var history = o.Versions.OrderBy(v => v.VersionNumber).SkipLast(1).ToList();
        HistoryHeader.Visibility = history.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        VersionHistoryList.ItemsSource = history;

        _auth.LogAction(_currentUser, "VIEW", $"Viewed ordinance {o.OrdinanceNumber}");
    }

    private static StackPanel MakeRow(string label, string value, bool bold = false, string? color = null)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
        panel.Children.Add(new TextBlock
        {
            Text = label + ": ",
            FontWeight = FontWeights.SemiBold,
            MinWidth = 100
        });
        panel.Children.Add(new TextBlock
        {
            Text = value,
            TextWrapping = TextWrapping.Wrap,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Foreground = color is null ? Brushes.Black : new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
            MaxWidth = 500
        });
        return panel;
    }
}