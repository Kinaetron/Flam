using System.Numerics;

namespace Flam.Shapes;

public record struct Rectangle
{
    private Vector2 _position;
    public Vector2 Position 
    {
        get => _position;
        set => _position = value;
    }
    public Vector2 Center
    {
        get => new(_position.X + (Width / 2), _position.Y + (Height / 2));
    }
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
    public float Left =>
        _position.X;
    public float Right =>
        _position.X + Width;
    public float Top => 
        _position.Y;
    public float Bottom => 
        _position.Y + Height;

    public Rectangle(float width, float height, float x, float y)
    {
        Width = width;
        Height = height;
        _position = new Vector2(x, y);
    }

    public Rectangle(float width, float height, Vector2 position)
    {
        Width = width;
        Height = height;
        _position = position;
    }
}
