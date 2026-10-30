using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using STOMS_SBV1_BOS_IME.Models;

namespace STOMS_SBV1_BOS_IME.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var flag = value is bool b && b;
            if (Invert) flag = !flag;
            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var isNull = value == null || (value is string s && string.IsNullOrWhiteSpace(s));
            if (Invert) isNull = !isNull;
            return isNull ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public class StockStatusToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is StockStatus status)
            {
                return status switch
                {
                    StockStatus.Normal => new SolidColorBrush(Color.FromRgb(16, 185, 129)),       // Emerald Green
                    StockStatus.LowStock => new SolidColorBrush(Color.FromRgb(245, 158, 11)),     // Amber
                    StockStatus.NearExpiry => new SolidColorBrush(Color.FromRgb(234, 88, 12)),    // Deep Orange
                    StockStatus.Expired => new SolidColorBrush(Color.FromRgb(239, 68, 68)),       // Crimson Red
                    StockStatus.Damaged => new SolidColorBrush(Color.FromRgb(168, 85, 247)),      // Purple
                    _ => new SolidColorBrush(Color.FromRgb(100, 116, 139))
                };
            }

            if (value is AlertSeverity severity)
            {
                return severity switch
                {
                    AlertSeverity.Info => new SolidColorBrush(Color.FromRgb(59, 130, 246)),       // Blue
                    AlertSeverity.Warning => new SolidColorBrush(Color.FromRgb(245, 158, 11)),    // Amber
                    AlertSeverity.Critical => new SolidColorBrush(Color.FromRgb(239, 68, 68)),    // Red
                    _ => new SolidColorBrush(Color.FromRgb(100, 116, 139))
                };
            }

            if (value is MovementType moveType)
            {
                return moveType switch
                {
                    MovementType.StockIn => new SolidColorBrush(Color.FromRgb(16, 185, 129)),     // Green
                    MovementType.StockOut => new SolidColorBrush(Color.FromRgb(239, 68, 68)),    // Red
                    MovementType.Transfer => new SolidColorBrush(Color.FromRgb(59, 130, 246)),    // Blue
                    MovementType.Return => new SolidColorBrush(Color.FromRgb(20, 184, 166)),      // Teal
                    MovementType.Damage => new SolidColorBrush(Color.FromRgb(168, 85, 247)),     // Purple
                    MovementType.Adjustment => new SolidColorBrush(Color.FromRgb(245, 158, 11)), // Amber
                    _ => new SolidColorBrush(Color.FromRgb(100, 116, 139))
                };
            }

            return new SolidColorBrush(Color.FromRgb(100, 116, 139));
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public class StockStatusToBgColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is StockStatus status)
            {
                return status switch
                {
                    StockStatus.Normal => (SolidColorBrush)Application.Current.FindResource("Theme.Status.Success.Bg"),
                    StockStatus.LowStock => (SolidColorBrush)Application.Current.FindResource("Theme.Status.Warning.Bg"),
                    StockStatus.NearExpiry => (SolidColorBrush)Application.Current.FindResource("Theme.Status.Orange.Bg"),
                    StockStatus.Expired => (SolidColorBrush)Application.Current.FindResource("Theme.Status.Danger.Bg"),
                    StockStatus.Damaged => (SolidColorBrush)Application.Current.FindResource("Theme.Status.Purple.Bg"),
                    _ => (SolidColorBrush)Application.Current.FindResource("Theme.Badge.Background")
                };
            }

            if (value is MovementType moveType)
            {
                return moveType switch
                {
                    MovementType.StockIn => (SolidColorBrush)Application.Current.FindResource("Theme.Status.Success.Bg"),
                    MovementType.StockOut => (SolidColorBrush)Application.Current.FindResource("Theme.Status.Danger.Bg"),
                    MovementType.Transfer => (SolidColorBrush)Application.Current.FindResource("Theme.Status.Info.Bg"),
                    MovementType.Return => (SolidColorBrush)Application.Current.FindResource("Theme.Status.Success.Bg"),
                    MovementType.Damage => (SolidColorBrush)Application.Current.FindResource("Theme.Status.Purple.Bg"),
                    MovementType.Adjustment => (SolidColorBrush)Application.Current.FindResource("Theme.Status.Warning.Bg"),
                    _ => (SolidColorBrush)Application.Current.FindResource("Theme.Badge.Background")
                };
            }

            return (SolidColorBrush)Application.Current.FindResource("Theme.Badge.Background");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public class OccupancyColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double percentage)
            {
                if (percentage >= 90.0) return new SolidColorBrush(Color.FromRgb(239, 68, 68));   // High - Red
                if (percentage >= 75.0) return new SolidColorBrush(Color.FromRgb(245, 158, 11));  // Moderate - Amber
                return new SolidColorBrush(Color.FromRgb(16, 185, 129));                         // Optimal - Green
            }
            return new SolidColorBrush(Color.FromRgb(100, 116, 139));
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public class ZoomToPercentConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double zoom)
            {
                return $"{(int)(zoom * 100)}%";
            }
            return "100%";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
