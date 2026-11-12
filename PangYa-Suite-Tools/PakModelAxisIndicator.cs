using System.Windows.Media.Media3D;
using Wpf = System.Windows;
using Controls = System.Windows.Controls;
using Media = System.Windows.Media;
using Shapes = System.Windows.Shapes;

namespace PangYa_Suite_Tools;

internal sealed class PakModelAxisIndicator : Controls.Canvas
{
    private const double Center = 42;
    private const double AxisLength = 25;

    private readonly Axis[] _axes;

    private sealed record Axis(Vector3D Direction, Wpf.Vector EndOnLabelDirection, Shapes.Line Line,
        Controls.TextBlock Label);

    public PakModelAxisIndicator()
    {
        Width = Height = 2 * Center;
        IsHitTestVisible = false;
        _axes =
        [
            AddAxis("X", new Vector3D(1, 0, 0), new Wpf.Vector(1, 0), Media.Brushes.LightCoral),
            AddAxis("Y", new Vector3D(0, 1, 0), new Wpf.Vector(0, -1), Media.Brushes.LightGreen),
            AddAxis("Z", new Vector3D(0, 0, 1), new Wpf.Vector(-1, 1), Media.Brushes.LightSkyBlue)
        ];
    }

    public void SetCameraDirection(Vector3D towardCamera, Vector3D cameraUp)
    {
        towardCamera.Normalize();
        Vector3D right = Vector3D.CrossProduct(cameraUp, towardCamera);
        right.Normalize();
        Vector3D up = Vector3D.CrossProduct(towardCamera, right);

        foreach (Axis axis in _axes)
        {
            var projection = new Wpf.Vector(Vector3D.DotProduct(axis.Direction, right),
                -Vector3D.DotProduct(axis.Direction, up));
            double x = Center + projection.X * AxisLength;
            double y = Center + projection.Y * AxisLength;
            axis.Line.X2 = x;
            axis.Line.Y2 = y;
            int depth = (int)(Vector3D.DotProduct(axis.Direction, towardCamera) * 100);
            SetZIndex(axis.Line, depth);

            // Keep the letter readable even when the camera is looking along an axis.
            Wpf.Vector labelDirection = projection.Length < 0.1 ? axis.EndOnLabelDirection : projection;
            labelDirection.Normalize();
            SetLeft(axis.Label, x + labelDirection.X * 10 - axis.Label.Width / 2);
            SetTop(axis.Label, y + labelDirection.Y * 10 - axis.Label.Height / 2);
            SetZIndex(axis.Label, 200 + depth);
        }
    }

    private Axis AddAxis(string name, Vector3D direction, Wpf.Vector endOnLabelDirection, Media.Brush color)
    {
        var line = new Shapes.Line
        {
            X1 = Center, Y1 = Center, Stroke = color, StrokeThickness = 2,
            StrokeStartLineCap = Media.PenLineCap.Round, StrokeEndLineCap = Media.PenLineCap.Round
        };
        var label = new Controls.TextBlock
        {
            Text = name, Foreground = color, FontWeight = Wpf.FontWeights.Bold, FontSize = 12,
            Width = 16, Height = 16, TextAlignment = Wpf.TextAlignment.Center
        };
        Children.Add(line);
        Children.Add(label);
        return new Axis(direction, endOnLabelDirection, line, label);
    }
}
