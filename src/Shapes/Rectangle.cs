using System.Numerics;

namespace Flam.Shapes;

public struct Rectangle
{
    public float Width { get; private set; }
    public float Height { get; private set; }
    public float Left { get => Position.X; }
    public float Right { get => Position.X + Width; }
    public float Top { get => Position.Y; }
    public float Bottom { get => Position.Y + Height; }
    public Vector2 Position { get; set; }

    public Rectangle(float width, float height, Vector2 position)
    {
        Width = width;
        Height = height;
        Position = position;
    }
}
