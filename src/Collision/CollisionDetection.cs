using Flam.Shapes;
using System.Numerics;

namespace Flam.Collision;

public class CollisionDetection
{
    public static bool RectangleCollidesRectangle(Rectangle rectangle1, Rectangle rectangle2) =>
        rectangle1.Left < rectangle2.Right &&
        rectangle1.Right > rectangle2.Left &&
        rectangle1.Top < rectangle2.Bottom &&
        rectangle1.Bottom > rectangle2.Top;


    public static bool CircleCollidesRectangle(Circle circle, Rectangle rectangle)
    {
        Vector2 clamp = Vector2.Zero;
        clamp.X = Math.Clamp(circle.X, rectangle.Left, rectangle.Right);
        clamp.Y = Math.Clamp(circle.Y, rectangle.Top, rectangle.Bottom);

        return CircleCollidePoint(circle, clamp);
    }

    private static bool CircleCollidePoint(Circle circle, Vector2 point)
    {
        var distance = circle.Position - point;
        return distance.Length() <= circle.Radius;
    }
}
