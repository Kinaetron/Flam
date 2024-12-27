using MoonWorks.Math;

namespace Flam.Math;

public static class FlamMathHelper
{
    public static float NormalizeAngle(float angle)
    {
        while (angle < 0) angle += MathHelper.TwoPi;
        while (angle >= MathHelper.TwoPi) angle -= MathHelper.TwoPi;

        return angle;
    }
}
