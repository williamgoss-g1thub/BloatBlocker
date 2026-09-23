using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace BloatBlocker.Converters;

/// <summary>
/// Builds a circular progress arc from (ProtectedCount, TotalCount). Fixed to a
/// 120x120 layout box, 50px radius, centered at (60,60) — matches the ring
/// drawn in MainWindow.xaml. WPF's ArcSegment can't represent a mathematically
/// complete circle (start point == end point is degenerate), so 100% is clamped
/// to 99.9% — visually indistinguishable from a full ring at this stroke width.
/// </summary>
public class ProtectionRingConverter : IMultiValueConverter
{
    private const double Cx = 60;
    private const double Cy = 60;
    private const double Radius = 50;

    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not int protectedCount || values[1] is not int total || total == 0)
            return Geometry.Empty;

        double percent = Math.Clamp((double)protectedCount / total, 0.0, 0.999);
        if (percent <= 0.0) return Geometry.Empty;

        double sweepDegrees = 360 * percent;
        Point ToPoint(double angleDeg)
        {
            double rad = Math.PI / 180 * (angleDeg - 90); // 0deg = top, clockwise
            return new Point(Cx + Radius * Math.Cos(rad), Cy + Radius * Math.Sin(rad));
        }

        var start = ToPoint(0);
        var end = ToPoint(sweepDegrees);
        bool isLargeArc = sweepDegrees > 180;

        var figure = new PathFigure { StartPoint = start, IsClosed = false };
        figure.Segments.Add(new ArcSegment(end, new Size(Radius, Radius), 0, isLargeArc, SweepDirection.Clockwise, true));

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
