using System;
using System.Collections.Generic;
using System.Text;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace LLOIS;

using LLOIS.Models;

/// <summary>
/// Returns the theme-aware background brush for an ordinance status pill.
/// Bind directly to the Status property (no MultiBinding needed).
/// </summary>
public class StatusToBrushConverter : IValueConverter
{
    public static readonly StatusToBrushConverter Instance = new();

    private static readonly Dictionary<OrdinanceStatus, string> _bgResourceKeys = new()
    {
        [OrdinanceStatus.InEffect]    = "StatusInEffectBgBrush",
        [OrdinanceStatus.Amended]     = "StatusAmendedBgBrush",
        [OrdinanceStatus.Repealed]    = "StatusRepealedBgBrush",
        [OrdinanceStatus.UnderReview] = "StatusReviewBgBrush",
        [OrdinanceStatus.Superseded]  = "StatusSupersededBgBrush",
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not OrdinanceStatus status)
            return Brushes.Transparent;

        var key = _bgResourceKeys.GetValueOrDefault(status, "StatusReviewBgBrush");
        return Application.Current.TryFindResource(key) as Brush ?? Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}