namespace LLOIS;

using System.Windows;
using LLOIS.Services;
using LLOIS.Views;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        PdfFontResolver.Apply();
        base.OnStartup(e);

        // Default to Light; user can toggle via the top-bar button.
        // To auto-detect the OS preference instead, replace with:
        //   ThemeService.Apply(IsSystemDark());
        ThemeService.Apply(dark: false);

        var shell = new ShellWindow();
        shell.Show();
    }

    /// <summary>
    /// Optional: detects the Windows 10/11 dark-mode registry setting.
    /// </summary>
    private static bool IsSystemDark()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser
                .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var val = key?.GetValue("AppsUseLightTheme");
            return val is int i && i == 0;
        }
        catch { return false; }
    }
}
