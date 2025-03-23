using Flam.Shapes;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;

namespace Flam.src.Collision;

public class CollisionNormals
{
    public static Vector2 RectangleToRectangleNormal(Rectangle rectA, Rectangle rectB)
    {
        var delta = rectA.Center - rectB.Center;

        var totalHalfWidth = (rectA.Width / 2) + (rectB.Width / 2);
        var totalHalfHeight = (rectA.Height / 2) + (rectB.Height / 2);

        var overlapX = totalHalfWidth - MathF.Abs(delta.X);
        var overlapY = totalHalfHeight - MathF.Abs(delta.Y);

        if(overlapX < overlapY)
        {
            return (delta.X < 0) ? new Vector2(-1, 0) : new Vector2(1, 0);
        }
        else
        {
            return (delta.Y < 0) ? new Vector2(0, -1) : new Vector2(0, 1);
        }
    }

    public static Vector2 CircleToCircleNormal(Circle circleA, Circle circleB)
    {
        var normal = circleA.Position - circleB.Position;

        if(normal != Vector2.Zero)
        {
            normal = Vector2.Normalize(normal);
        }

        return normal;
    }

    public static Vector2 CircleToRectangleNormal(Circle circle, Rectangle rectangle)
    {
        var closestX = MathF.Max(rectangle.Left, MathF.Min(circle.Position.X, rectangle.Right));
        var closestY = MathF.Max(rectangle.Top, MathF.Min(circle.Position.Y, rectangle.Bottom));

        var closestPoint = new Vector2(closestX, closestY);
        var normal = circle.Position - closestPoint;

        if(normal != Vector2.Zero)
        {
            normal = Vector2.Normalize(normal);
        }

        return normal;
    }
}
