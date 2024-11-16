using System.Numerics;

namespace Flam.Shapes;

public struct Circle
{
    private Vector2 _position;
    public Vector2 Position
    {
        get => _position;
        set => _position = value;
    }

    public readonly float Radius;

    public float X
    {
        get => _position.X;
        set => _position = new Vector2(value, _position.Y);
    }
    public float Y
    {
        get => _position.Y;
        set => _position = new Vector2(_position.X, value);
    }

    public Circle(float radius, Vector2 position)
    {
        Radius = radius;
        _position = position;
    }
}
