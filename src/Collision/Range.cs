namespace Flam.Collision;

internal struct Range
{
    internal float Minimum;
    internal float Maximum;

    internal Range(float minimum, float maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
    }
}
