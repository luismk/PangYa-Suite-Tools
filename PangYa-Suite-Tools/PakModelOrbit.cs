using System.Windows.Media.Media3D;

namespace PangYa_Suite_Tools;

internal sealed class PakModelOrbit
{
    private const double DragRadians = 0.012;
    private double _yaw;

    public PakModelOrbit() => Reset();

    public Vector3D Direction { get; private set; }
    public Vector3D Up { get; private set; }
    public Keys LockedAxis { get; private set; }

    public bool ToggleAxis(Keys key)
    {
        if (key is not (Keys.X or Keys.Y or Keys.Z))
            return false;
        LockedAxis = LockedAxis == key ? Keys.None : key;
        return true;
    }

    public void Reset()
    {
        _yaw = 0.45;
        Direction = DirectionFromAngles(_yaw, 0.25);
        Up = Vector3D.CrossProduct(Direction, HorizontalRight(_yaw));
    }

    public void Drag(double horizontal, double vertical)
    {
        if (LockedAxis != Keys.None)
        {
            double pixels = LockedAxis == Keys.X ? vertical : horizontal;
            Vector3D axis = LockedAxis switch
            {
                Keys.X => new Vector3D(1, 0, 0),
                Keys.Y => new Vector3D(0, 1, 0),
                _ => new Vector3D(0, 0, 1)
            };
            var rotation = Matrix3D.Identity;
            rotation.Rotate(new Quaternion(axis, -pixels * DragRadians * 180 / Math.PI));
            Direction = rotation.Transform(Direction);
            Up = rotation.Transform(Up);
            Direction.Normalize();
            Up.Normalize();
            if (Direction.X * Direction.X + Direction.Z * Direction.Z > 1e-12)
                _yaw = Math.Atan2(Direction.X, Direction.Z);
            return;
        }

        double pitch = Math.Asin(Math.Clamp(Direction.Y, -1, 1));
        Vector3D right = HorizontalRight(_yaw);
        Vector3D verticalUp = Vector3D.CrossProduct(Direction, right);
        double rollRight = Vector3D.DotProduct(Up, right);
        double rollUp = Vector3D.DotProduct(Up, verticalUp);
        _yaw -= horizontal * DragRadians;
        pitch = Math.Clamp(pitch + vertical * DragRadians, -1.5, 1.5);
        Direction = DirectionFromAngles(_yaw, pitch);
        right = HorizontalRight(_yaw);
        Up = rollRight * right + rollUp * Vector3D.CrossProduct(Direction, right);
        Up.Normalize();
    }

    private static Vector3D DirectionFromAngles(double yaw, double pitch) =>
        new(Math.Sin(yaw) * Math.Cos(pitch), Math.Sin(pitch), Math.Cos(yaw) * Math.Cos(pitch));

    private static Vector3D HorizontalRight(double yaw) => new(Math.Cos(yaw), 0, -Math.Sin(yaw));
}
