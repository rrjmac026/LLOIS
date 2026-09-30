namespace LLOIS.Views;

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LLOIS.Services;

public partial class LoginView : UserControl
{
    private readonly ApiClient _api;
    private readonly IAuthService _auth;

    public event Action<ApiUser, ApiClient>? LoginSucceeded;

    public LoginView(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        _auth = new AuthService(api);
    }

    private async void TryLogin()
    {
        string username = UsernameBox.Text.Trim();
        string password = PasswordBox.Password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ErrorText.Text = "Please fill in all fields.";
            ErrorBanner.Visibility = Visibility.Visible;
            return;
        }

        LoginButton.IsEnabled = false;
        ErrorBanner.Visibility = Visibility.Collapsed;

        try
        {
            var user = await _auth.LoginAsync(username, password, rememberMe: true);
            LoginButton.IsEnabled = true;

            LoginSucceeded?.Invoke(user, _api);
        }
        catch (UnauthorizedAccessException)
        {
            LoginButton.IsEnabled = true;
            ErrorText.Text = "Invalid username or password.";
            ErrorBanner.Visibility = Visibility.Visible;
        }
        catch (InvalidOperationException ex)
        {
            LoginButton.IsEnabled = true;
            ErrorText.Text = ex.Message;
            ErrorBanner.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            LoginButton.IsEnabled = true;
            if (ConnectionFailureHandler.HandleIfApiFailure(ex))
                return;

            MessageBox.Show($"Unable to log in:\n{ex.Message}", "Login Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e) => TryLogin();

    private void Field_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) TryLogin();
    }
}