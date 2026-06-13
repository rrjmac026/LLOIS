namespace LLOIS;

using System.Windows;
using LLOIS.Views;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var login = new LoginWindow();
        if (login.ShowDialog() == true && login.LoggedInUser is not null)
        {
            var main = new MainWindow(login.LoggedInUser, login.Db);
            main.Show();
        }
        else
        {
            Shutdown();
        }
    }
}