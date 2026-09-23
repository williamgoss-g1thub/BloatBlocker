using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using BloatBlocker.Models;
// UseWindowsForms (for the tray icon) makes the SDK auto-add System.Drawing
// and System.Windows.Forms as global usings project-wide, which collide with
// WPF's own Color/ColorConverter/Binding. Alias them explicitly here so this
// file always means the WPF ones, regardless of what else the project adds.
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Binding = System.Windows.Data.Binding;

namespace BloatBlocker.Converters;

public class StatusToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ActionStatus status
            ? status switch
            {
                ActionStatus.Protected => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#122A24")!),
                ActionStatus.Reverted => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2B2313")!),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D2129")!)
            }
            : Binding.DoNothing;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class StatusToForegroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ActionStatus status
            ? status switch
            {
                ActionStatus.Protected => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1FAE85")!),
                ActionStatus.Reverted => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8A33D")!),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9AA0AC")!)
            }
            : Binding.DoNothing;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Shows the Apply button while a feature isn't protected yet (NotApplied or Reverted).</summary>
public class StatusToApplyVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ActionStatus status && status != ActionStatus.Protected
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Shows the Revert button once a feature is protected.</summary>
public class StatusToRevertVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ActionStatus status && status == ActionStatus.Protected
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
