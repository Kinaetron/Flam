using System.Numerics;

namespace Flam.Shapes;

public record struct Triangle
{
    public Vector2 Point1;

    public Vector2 Point2;

    public Vector2 Point3;

    public Triangle(Vector2 point1, Vector2 point2, Vector2 point3)
    {
        Point1 = point1;
        Point2 = point2;
        Point3 = point3;
    }
}
