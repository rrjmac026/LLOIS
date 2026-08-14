using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS;

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using LLOIS.Models;

/// <summary>
/// Returns a brush for the Type pill background or foreground.
/// Parameter = "bg" or "fg".
/// </summary>
public class OrdinanceTypeColorConverter : IValueConverter
{
    public static readonly OrdinanceTypeColorConverter Instance = new();

    // Light-mode palette — readable, distinct, accessible for older eyes
    private static readonly Dictionary<TypeOfLaw, (Color Bg, Color Fg)> _colors = new()
    {
        [TypeOfLaw.Resolution] = (Color.FromRgb(0xEF, 0xF6, 0xFF), Color.FromRgb(0x1D, 0x4E, 0xD8)), // blue
        [TypeOfLaw.Ordinance]  = (Color.FromRgb(0xDC, 0xFC, 0xE7), Color.FromRgb(0x14, 0x53, 0x2D)), // green
        [TypeOfLaw.Minutes]    = (Color.FromRgb(0xF3, 0xE8, 0xFF), Color.FromRgb(0x6B, 0x21, 0xA8)), // purple
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not TypeOfLaw type) return DependencyProperty.UnsetValue;
        if (!_colors.TryGetValue(type, out var pair)) return DependencyProperty.UnsetValue;

        bool isBg = parameter?.ToString()?.ToLower() == "bg";
        return new SolidColorBrush(isBg ? pair.Bg : pair.Fg);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}