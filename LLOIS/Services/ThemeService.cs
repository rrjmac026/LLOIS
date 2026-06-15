using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Services;

using System.Windows;

/// <summary>
/// Manages switching between Light and Dark resource dictionaries at runtime.
/// </summary>
public static class ThemeService
{
    private const string LightThemePath = "Themes/LightTheme.xaml";
    private const string DarkThemePath  = "Themes/DarkTheme.xaml";

    public static bool IsDark { get; private set; } = false;

    /// <summary>
    /// Raised whenever the theme changes so any window can react (e.g. redraw
    /// status badge colors that are set in code-behind).
    /// </summary>
    public static event Action<bool>? ThemeChanged;

    public static void Apply(bool dark)
    {
        IsDark = dark;

        var dicts    = Application.Current.Resources.MergedDictionaries;
        var themePath = dark ? DarkThemePath : LightThemePath;

        // Remove the old theme dictionary, add the new one
        var existing = dicts.FirstOrDefault(d =>
            d.Source?.OriginalString.Contains("Theme.xaml") == true);
        if (existing is not null)
            dicts.Remove(existing);

        dicts.Insert(0, new ResourceDictionary
        {
            Source = new Uri(themePath, UriKind.Relative)
        });

        ThemeChanged?.Invoke(dark);
    }

    public static void Toggle() => Apply(!IsDark);
}
