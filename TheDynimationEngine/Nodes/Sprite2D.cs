// File: Nodes/Sprite2D.cs
using TheDynimationEngine.Rendering; // Needs Texture
using SkiaSharp;
using System.Numerics; // For Vector2 offset/scale
using System; // For IDisposable used by SKPaint components

namespace TheDynimationEngine.Nodes
{
    /// <summary>
    /// A Node2D that draws a Texture at its position, rotation, and scale.
    /// Can draw the entire texture or a specific region (for sprite sheets).
    /// </summary>
    public class Sprite2D : Node2D
    {
        private Texture? _texture = null;
        public Texture? Texture { get => _texture; set => _texture = value; }

        private bool _centered = true;
        public bool Centered { get => _centered; set => _centered = value; }

        private Vector2 _offset = Vector2.Zero;
        public Vector2 Offset { get => _offset; set => _offset = value; }

        private bool _regionEnabled = false;
        public bool RegionEnabled { get => _regionEnabled; set => _regionEnabled = value; }

        private SKRectI _regionRect = SKRectI.Empty;
        public SKRectI RegionRect { get => _regionRect; set => _regionRect = value; }

        private SKColor _modulate = SKColors.White;
        public SKColor Modulate { get => _modulate; set { if(_modulate != value) { _modulate = value; _isPaintDirty = true; } } }

        private bool _flipH = false;
        public bool FlipH { get => _flipH; set => _flipH = value; }

        private bool _flipV = false;
        public bool FlipV { get => _flipV; set => _flipV = value; }

        // --- Reverted: Remove SKSamplingOptions for now, use FilterQuality ---
        // private SKSamplingOptions _samplingOptions = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
        // public SKSamplingOptions SamplingOptions { get => _samplingOptions; set => _samplingOptions = value; }

        // --- Added FilterQuality property ---
        private SKFilterQuality _filterQuality = SKFilterQuality.None; // Default: Pixelated look (Nearest Neighbor)
        /// <summary>
        /// Gets or sets the filtering quality when scaling/rotating the sprite.
        /// None/Low = Nearest Neighbor (Pixelated), Medium = Bilinear, High = Bicubic (Smoothest).
        /// Note: This property is obsolete in newer SkiaSharp versions, prefer SKSamplingOptions with different drawing methods if possible.
        /// </summary>
        public SKFilterQuality FilterQuality
        {
             get => _filterQuality;
             set { if(_filterQuality != value) { _filterQuality = value; _isPaintDirty = true; } } // Mark dirty
        }

        // Paint object
        private SKPaint _paint = new SKPaint { IsAntialias = false };
        private SKColor _lastModulateColor = SKColors.Transparent;
        private SKFilterQuality _lastFilterQuality = (SKFilterQuality)(-1); // Invalid initial value to force update
        private bool _isPaintDirty = true;

        public override void DrawSelf(SKCanvas canvas)
        {
            if (Texture?.Bitmap == null || !Texture.IsValid) return;

            // Update paint object only if Modulate color or FilterQuality changed
            if (_isPaintDirty || _lastModulateColor != Modulate || _lastFilterQuality != FilterQuality)
            {
                _paint.ColorFilter?.Dispose();

                if (Modulate == SKColors.White)
                {
                    _paint.ColorFilter = null;
                    _paint.Color = SKColors.White;
                }
                else
                {
                    _paint.ColorFilter = SKColorFilter.CreateBlendMode(Modulate, SKBlendMode.Modulate);
                    _paint.Color = SKColors.White;
                }

                // --- Apply FilterQuality, suppressing the obsolescence warning ---
                #pragma warning disable CS0618 // Type or member is obsolete
                _paint.FilterQuality = FilterQuality;
                #pragma warning restore CS0618 // Type or member is obsolete

                _lastModulateColor = Modulate;
                _lastFilterQuality = FilterQuality;
                _isPaintDirty = false;
            }

            SKRect sourceRect; // Use SKRect for drawing
            if (RegionEnabled && !RegionRect.IsEmpty)
            {
                sourceRect = SKRect.Create(RegionRect.Left, RegionRect.Top, RegionRect.Width, RegionRect.Height);
            }
            else
            {
                sourceRect = SKRect.Create(0, 0, Texture.Width, Texture.Height);
            }

            float destWidth = sourceRect.Width;
            float destHeight = sourceRect.Height;
            float drawOffsetX = Offset.X + (Centered ? -destWidth / 2f : 0f);
            float drawOffsetY = Offset.Y + (Centered ? -destHeight / 2f : 0f);

            int flipSaveCount = 0;
            if (FlipH || FlipV)
            {
                flipSaveCount = canvas.Save();
                float scaleX = FlipH ? -1f : 1f; float scaleY = FlipV ? -1f : 1f;
                float pivotX = drawOffsetX + destWidth / 2f; float pivotY = drawOffsetY + destHeight / 2f;
                canvas.Translate(pivotX, pivotY); canvas.Scale(scaleX, scaleY); canvas.Translate(-pivotX, -pivotY);
            }

            var destRect = SKRect.Create(drawOffsetX, drawOffsetY, destWidth, destHeight);

            // --- CORRECTED DrawBitmap call (4 arguments) ---
            canvas.DrawBitmap(Texture.Bitmap, sourceRect, destRect, _paint);

            if (FlipH || FlipV)
            {
                canvas.RestoreToCount(flipSaveCount);
            }
        }
    }
}