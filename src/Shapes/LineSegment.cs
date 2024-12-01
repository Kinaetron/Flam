using System.Numerics;

namespace Flam.Shapes;

public struct LineSegment
{
    private Vector2 _point1;
    public Vector2 Point1
    {
        get => _point1;
        set => _point1 = value;
    }

    private Vector2 _point2;
    public Vector2 Point2
    {
        get => _point2;
        set => _point2 = value;
    }

    public LineSegment(Vector2 point1, Vector2 point2)
    {
        _point1 = point1;
        _point2 = point2;
    }
}
