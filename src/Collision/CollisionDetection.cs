using Flam.Math;
using Flam.Shapes;
using System.Numerics;

namespace Flam.Collision;

using MathSystem = System.Math;

public struct RaycastResult
{
    public bool Hit;
    public float THitNear;
    public Vector2 ContactPoint;
    public Vector2 Normal;
}

public class CollisionDetection
{
    public static bool TriangleCollidesRectangle(Triangle triangle, Rectangle rectangle)
    {
        var rectanglePoints = new Vector2[]
        {
            new (rectangle.Left, rectangle.Top),
            new (rectangle.Right, rectangle.Top),
            new (rectangle.Right, rectangle.Bottom),
            new (rectangle.Left, rectangle.Bottom)
        };

        var trianglePoints = new Vector2[]
        {
            triangle.Point1,
            triangle.Point2,
            triangle.Point3
        };

      
        var rectangleNormals = new Vector2[]
        {
            FlamMathHelper.Normal(rectanglePoints[1] - rectanglePoints[0]),
            FlamMathHelper.Normal(rectanglePoints[2] - rectanglePoints[1]),
        };


        var triangleNormals = new Vector2[]
        {
            FlamMathHelper.Normal(trianglePoints[1] - trianglePoints[0]),
            FlamMathHelper.Normal(trianglePoints[2] - trianglePoints[1]),
            FlamMathHelper.Normal(trianglePoints[0] - trianglePoints[2])
        };

        var axes = rectangleNormals.Concat(triangleNormals);

        foreach (var axis in axes)
        {
            var rectProjection = ProjectShape(rectanglePoints, axis);
            var triProjection = ProjectShape(trianglePoints, axis);

            if (!IntervalsOverlap(rectProjection, triProjection))
            {
                return false;
            }
        }

        return true;
    }

    public static bool RectangleCollidesRectangle(Rectangle rectangle1, Rectangle rectangle2) =>
        rectangle1.Left < rectangle2.Right &&
        rectangle1.Right > rectangle2.Left &&
        rectangle1.Top < rectangle2.Bottom &&
        rectangle1.Bottom > rectangle2.Top;

    public static bool RectangleCollidesPoint(Rectangle rectangle, Vector2 point) =>
        point.X >= rectangle.Left && 
        point.Y >= rectangle.Top && 
        point.X < rectangle.Right && 
        point.Y < rectangle.Bottom;

    public static RaycastResult MovingRectangleCollidesRectangle(Rectangle moving, Rectangle target, Vector2 velocity)
    {
        var result = new RaycastResult { Hit = false, THitNear = 0 };

        if (velocity == Vector2.Zero)
        {
            return result;
        }

        var invVelocity = new Vector2(
            velocity.X != 0 ? 1.0f / velocity.X : float.MaxValue,
            velocity.Y != 0 ? 1.0f / velocity.Y : float.MaxValue
            );

        var tNear = new Vector2(
            (target.Left - moving.Right) * invVelocity.X,
            (target.Top - moving.Bottom) * invVelocity.Y);

        var tFar = new Vector2(
            (target.Right - moving.Left) * invVelocity.X,
            (target.Bottom - moving.Top) * invVelocity.Y);

        if (tNear.X > tFar.X) (tNear.X, tFar.X) = (tFar.X, tNear.X);
        if (tNear.Y > tFar.Y) (tNear.Y, tFar.Y) = (tFar.Y, tNear.Y);

        if(tNear.X > tFar.X || tNear.Y > tFar.Y)
        {
            return result;
        }

        result.THitNear = MathF.Max(tNear.X, tNear.Y);
        var tHitFar = MathF.Min(tFar.X, tFar.Y);

        if(tHitFar < 0 || result.THitNear > 1)
        {
            return result;
        }

        result.Hit = true;
        result.ContactPoint = moving.Position + velocity * result.THitNear;

        if(tNear.X > tNear.Y)
        {
            result.Normal = velocity.X < 0 ? new Vector2(1, 0) : new Vector2(-1, 0);
        }
        else
        {
            result.Normal = velocity.Y < 0 ? new Vector2(0, 1) : new Vector2(0, -1);
        }

        return result;
    }

    public static bool LineSegmentCollidesRectangle(LineSegment lineSegment, Rectangle rectangle)
    {
        var rectangleRange = new Range(rectangle.Left, rectangle.Right);
        var lineSegmentRange = new Range(lineSegment.Point1.X, lineSegment.Point2.X);

        lineSegmentRange = SortedRange(lineSegmentRange);

        if (!OverlappingRanges(rectangleRange, lineSegmentRange))
            return false;

        rectangleRange.Minimum = rectangle.Top;
        rectangleRange.Maximum = rectangle.Bottom;
        lineSegmentRange.Minimum = lineSegment.Point1.Y;
        lineSegmentRange.Maximum = lineSegment.Point2.Y;
        lineSegmentRange = SortedRange(lineSegmentRange);

        if (!OverlappingRanges(rectangleRange, lineSegmentRange))
            return false;

        var lineBase = lineSegment.Point1;
        var lineDirection = Vector2.Normalize(lineSegment.Point2 - lineSegment.Point1);
        var lineNormal = new Vector2(-lineDirection.Y, lineDirection.X);

        var corner1 = rectangle.Position;
        var corner2 = corner1 + new Vector2(rectangle.Width, rectangle.Height);
        var corner3 = new Vector2(corner2.X, corner1.Y);
        var corner4 = new Vector2(corner1.X, corner2.Y);

        corner1 -= lineBase;
        corner2 -= lineBase;
        corner3 -= lineBase;
        corner4 -= lineBase;

        var dotProduct1 = Vector2.Dot(lineNormal, corner1);
        var dotProduct2 = Vector2.Dot(lineNormal, corner2);
        var dotProduct3 = Vector2.Dot(lineNormal, corner3);
        var dotProduct4 = Vector2.Dot(lineNormal, corner4);

        return (dotProduct1 * dotProduct2 <= 0) || 
               (dotProduct2 * dotProduct3 <= 0) ||
               (dotProduct3 * dotProduct4 <= 0);
    }

    public static bool CircleCollidesRectangle(Circle circle, Rectangle rectangle)
    {
        Vector2 clamp = Vector2.Zero;
        clamp.X = MathSystem.Clamp(circle.X, rectangle.Left, rectangle.Right);
        clamp.Y = MathSystem.Clamp(circle.Y, rectangle.Top, rectangle.Bottom);

        return CircleCollidePoint(circle, clamp);
    }

    private static bool CircleCollidePoint(Circle circle, Vector2 point)
    {
        var distance = circle.Position - point;
        return distance.Length() <= circle.Radius;
    }

    private static bool OverlappingRanges(Range range1, Range range2) =>
        range2.Minimum <= range1.Maximum && range1.Minimum <= range2.Maximum;

    private static Range SortedRange(Range range)
    {
        var sorted = range;
        if(range.Minimum > range.Maximum)
        {
            sorted.Minimum = range.Maximum;
            sorted.Maximum = range.Minimum;
        }

        return sorted;
    }

    private static (float min, float max) ProjectShape(Vector2[] points, Vector2 axis)
    {
        float min = float.MaxValue;
        float max = float.MinValue;

        foreach (var point in points)
        {
            float projection = Vector2.Dot(point, axis);
            min = MathSystem.Min(min, projection);
            max = MathSystem.Max(max, projection);
        }

        return (min, max);
    }

    private static bool IntervalsOverlap((float min, float max) a, (float min, float max) b) =>
        a.max >= b.min && b.max >= a.min;
}
