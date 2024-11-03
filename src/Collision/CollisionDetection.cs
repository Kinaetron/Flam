using Flam.Shapes;

namespace Flam.Collision;

public class CollisionDetection
{
    public static bool RectangleCollidesRectangle(Rectangle rectangle1, Rectangle rectangle2) =>
        rectangle1.Left < rectangle2.Right &&
        rectangle1.Right > rectangle2.Left &&
        rectangle1.Top < rectangle2.Bottom &&
        rectangle1.Bottom > rectangle2.Top;
}
