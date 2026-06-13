namespace LLOIS;

using System.Windows;
using LLOIS.Views;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        PdfFontResolver.Apply();
        
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown; // prevent premature exit

        var login = new LoginWindow();
        if (login.ShowDialog() == true && login.LoggedInUser is not null)
        {
            ShutdownMode = ShutdownMode.OnLastWindowClose; // restore normal behavior
            var main = new MainWindow(login.LoggedInUser, login.Db);
            main.Show();
        }
        else
        {
            Shutdown();
        }
    }
}