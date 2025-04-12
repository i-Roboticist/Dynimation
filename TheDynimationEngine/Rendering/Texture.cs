// File: Rendering/Texture.cs
using System;
using System.IO;
using SkiaSharp;

namespace TheDynimationEngine.Rendering
{
    public class Texture : IDisposable
    {
        private SKBitmap? _bitmap;
        private bool _isDisposed = false;

        public SKBitmap? Bitmap => _bitmap;
        public int Width => _bitmap?.Width ?? 0;
        public int Height => _bitmap?.Height ?? 0;
        public System.Numerics.Vector2 Size => _bitmap != null ? new System.Numerics.Vector2(Width, Height) : System.Numerics.Vector2.Zero;
        public bool IsValid => _bitmap != null && !_isDisposed;

        // --- Constructor remains PRIVATE ---
        private Texture(SKBitmap bitmap)
        {
            _bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
        }

        // --- Removed internal static CreateFromBitmap_ForTesting method ---

        public static Texture? LoadFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                // Console.WriteLine($"Error: Texture file not found: {filePath}"); // Reduce noise
                return null;
            }
            try
            {
                using var stream = File.OpenRead(filePath);
                // Use DecodeBounds first to avoid loading large images just for info? Maybe later.
                SKBitmap? loadedBitmap = SKBitmap.Decode(stream);
                if (loadedBitmap == null)
                {
                    Console.WriteLine($"Error: Failed to decode texture file: {filePath}");
                    return null;
                }
                 // Ensure RGBA8888 Premul format for consistency
                 if (loadedBitmap.ColorType != SKColorType.Rgba8888 || loadedBitmap.AlphaType != SKAlphaType.Premul)
                 {
                     SKImageInfo desiredInfo = loadedBitmap.Info.WithColorType(SKColorType.Rgba8888).WithAlphaType(SKAlphaType.Premul);
                     var convertedBitmap = new SKBitmap(desiredInfo);
                     using (var canvas = new SKCanvas(convertedBitmap)) { canvas.DrawBitmap(loadedBitmap, 0, 0); }
                     loadedBitmap.Dispose();
                     loadedBitmap = convertedBitmap;
                 }
                // Use the PRIVATE constructor internally
                return new Texture(loadedBitmap);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading texture file '{filePath}': {ex.Message}");
                return null;
            }
        }

        public static Texture CreatePlaceholder(int width, int height, SKColor color)
        {
             if (width <= 0) width = 1; if (height <= 0) height = 1;
             var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
             var bitmap = new SKBitmap(info);
             using (var canvas = new SKCanvas(bitmap)) { canvas.Clear(color); }
             // Use the PRIVATE constructor internally
             return new Texture(bitmap);
        }

        // --- IDisposable Implementation ---
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing) { _bitmap?.Dispose(); }
                _bitmap = null;
                _isDisposed = true;
            }
        }
         ~Texture() { Dispose(false); }
    }
}