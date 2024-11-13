using System.Numerics;

namespace Flam.Shapes;

public struct Rectangle
{
    private Vector2 _position;
    public Vector2 Position 
    {
        get => _position;
        set => _position = value;
    }

    public readonly Vector2 Center;

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
    public float Width { get; private set; }
    public float Height { get; private set; }
    public readonly float Left =>
        _position.X;
    public readonly float Right =>
        _position.X + Width;
    public readonly float Top => 
        _position.Y;
    public readonly float Bottom => 
        _position.Y + Height;

    public Rectangle(float width, float height, Vector2 position)
    {
        Width = width;
        Height = height;
        _position = position;
        Center = new Vector2(position.X + (width / 2), position.Y + (height / 2));
    }
}
