// File: Nodes/ShapeRenderer2D.cs
using TheDynimationEngine.Core; // We don't directly use Core here, but good practice
using SkiaSharp;
using System.Numerics; // For Vector2 size

namespace TheDynimationEngine.Nodes
{
    /// <summary>
    /// Enum defining the types of shapes that can be rendered.
    /// </summary>
    public enum ShapeType
    {
        Rectangle,
        Circle,
        // Future: Ellipse, Polygon, Line, etc.
    }

    /// <summary>
    /// A Node2D that renders a basic 2D geometric shape (Rectangle or Circle)
    /// using its transform (position, rotation, scale).
    /// </summary>
    public class ShapeRenderer2D : Node2D
    {
        private SKColor _color = SKColors.White;
        /// <summary>
        /// The fill color of the shape.
        /// </summary>
        public SKColor Color
        {
            get => _color;
            set => _color = value;
        }

        private ShapeType _shapeType = ShapeType.Rectangle;
        /// <summary>
        /// The type of shape to render (Rectangle or Circle).
        /// </summary>
        public ShapeType ShapeType
        {
            get => _shapeType;
            set => _shapeType = value;
        }

        private Vector2 _size = new Vector2(100, 100); // Default size
        /// <summary>
        /// The base size of the shape before scaling.
        /// For Rectangle: Width and Height.
        /// For Circle: Diameter (Radius is Size.X / 2 or Size.Y / 2). Uses Size.X primarily.
        /// </summary>
        public Vector2 Size
        {
            get => _size;
            set => _size = value;
        }

        private bool _centered = true;
        /// <summary>
        /// If true, the shape's origin (for position and rotation) is its center.
        /// If false, the origin is the top-left corner.
        /// </summary>
        public bool Centered
        {
            get => _centered;
            set => _centered = value;
        }

        // We can reuse a single SKPaint object for efficiency if properties don't change often.
        // However, creating it per draw is simpler for now and handles color changes easily.
        // private SKPaint _paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };

        /// <summary>
        /// Overrides the Node2D DrawSelf method to render the specified shape.
        /// This method is called after the Node2D has applied its global transform to the canvas.
        /// Therefore, all drawing here is relative to the node's local origin (0,0).
        /// </summary>
        /// <param name="canvas">The transformed canvas to draw on.</param>
        public override void DrawSelf(SKCanvas canvas)
        {
            // Create paint object for this draw call
            using (var paint = new SKPaint())
            {
                paint.Color = Color;
                paint.IsAntialias = true;
                paint.Style = SKPaintStyle.Fill; // Only fill for now

                float width = Size.X;
                float height = Size.Y;
                float offsetX = Centered ? -width / 2f : 0f;
                float offsetY = Centered ? -height / 2f : 0f;

                switch (ShapeType)
                {
                    case ShapeType.Rectangle:
                        // Draw rectangle relative to local origin (0,0)
                        // Offset if centered
                        var rect = SKRect.Create(offsetX, offsetY, width, height);
                        canvas.DrawRect(rect, paint);
                        break;

                    case ShapeType.Circle:
                        // Use width as diameter, calculate radius
                        float radius = width / 2f;
                        // Center of circle is always relative to local origin (0,0)
                        // If centered=true, draw at (0,0). If centered=false, draw at (radius, radius)
                        // to make top-left the origin. Let's stick to drawing centered on origin for circle for simplicity.
                        float circleCenterX = Centered ? 0f : radius;
                        float circleCenterY = Centered ? 0f : radius; // Use radius Y if width/height differ? No, use X for radius.
                        // Let's simplify: Circle is *always* drawn centered at the local origin (0,0) regardless of Centered flag?
                        // No, let's respect the Centered flag for consistency.
                        // If Centered = true, origin is center, draw circle at (0,0).
                        // If Centered = false, origin is top-left, draw circle at (radius, radius).
                        circleCenterX = Centered ? 0f : radius;
                        circleCenterY = Centered ? 0f : radius;

                        canvas.DrawCircle(circleCenterX, circleCenterY, radius, paint);
                        break;

                    // Add cases for other shapes later
                }
            } // Dispose paint object
        }
    }
}