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
        clamp.X = Math.Clamp(circle.X, rectangle.Left, rectangle.Right);
        clamp.Y = Math.Clamp(circle.Y, rectangle.Top, rectangle.Bottom);

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
}
