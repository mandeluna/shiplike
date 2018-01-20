using Microsoft.Xna.Framework;

namespace Shiplike
{
    public enum ShapeType { Rectangle, Ellipse, Polygon };

    public class Shape
    {
        public ShapeType Type { get; }
        public Rectangle Bounds { get; }

        public Shape(Rectangle rect)
        {
            Type = ShapeType.Rectangle;
            Bounds = rect;
        }
    }
}