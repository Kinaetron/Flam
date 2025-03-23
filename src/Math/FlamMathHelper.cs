using MoonWorks.Math;
using System.Numerics;
using MathSystem = System.Math;

namespace Flam.Math;

public static class FlamMathHelper
{
    public static float NormalizeAngle(float angle)
    {
        while (angle < 0) angle += MathHelper.TwoPi;
        while (angle >= MathHelper.TwoPi) angle -= MathHelper.TwoPi;

        return angle;
    }

    public static float DegreesToRadians(float degrees) =>
         degrees * (float) (MathSystem.PI / 180);

    public static bool IsInRange(float value1, float value2, float max)
    {
        var difference = MathSystem.Abs(value1 - value2);
        return difference <= max;
    }

    public static Vector2 Normal(Vector2 value) =>
        Vector2.Normalize(new Vector2(value.Y, -value.X));

    public static Vector2 Project(this Vector2 project, Vector2 onto)
    {
        float dotProduct = Vector2.Dot(project, onto);
        float lengthSquared = onto.LengthSquared();
        return (lengthSquared > 0) ? (dotProduct / lengthSquared) * project : Vector2.Zero;
    }
}
