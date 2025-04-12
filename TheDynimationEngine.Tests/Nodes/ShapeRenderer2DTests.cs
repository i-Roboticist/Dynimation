// File: TheDynimationEngine.Tests/ShapeRenderer2DTests.cs
using Xunit;
using TheDynimationEngine.Core;
using TheDynimationEngine.Nodes;
using SkiaSharp;
using System.Numerics;
using System.IO; // Required for Path and File operations
using Xunit.Abstractions; // Required for test output helper

namespace TheDynimationEngine.Tests
{
    public class ShapeRenderer2DTests
    {
        // --- Test Output Setup ---
        private readonly ITestOutputHelper _output; // Allows writing messages visible in test runner
        private const string OutputDirectory = "TestOutput/ShapeRenderer2D";

        public ShapeRenderer2DTests(ITestOutputHelper output)
        {
            _output = output;
            // Ensure the output directory exists before tests run
            Directory.CreateDirectory(OutputDirectory);
            _output.WriteLine($"Ensured test output directory exists: {Path.GetFullPath(OutputDirectory)}");
        }

        // --- Helper Methods ---
        private (SKSurface, SKCanvas) CreateTestCanvas(int width = 100, int height = 100)
        {
            var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            var surface = SKSurface.Create(info);
            if (surface == null) throw new InvalidOperationException("Could not create test SKSurface.");
            return (surface, surface.Canvas);
        }

        private void SaveCanvasToFile(SKSurface surface, string testName)
        {
            string fileName = $"{testName}.png";
            string filePath = Path.Combine(OutputDirectory, fileName);

            try
            {
                using (var image = surface.Snapshot()) // Get image from surface
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100)) // Encode as PNG
                using (var stream = File.OpenWrite(filePath)) // Open file stream
                {
                    data.SaveTo(stream); // Save PNG data to file
                }
                _output.WriteLine($"Saved test output image: {filePath}");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error saving test output image '{filePath}': {ex.Message}");
            }
        }

        // --- Basic Tests (Unchanged) ---
        [Fact]
        public void ShapeRenderer2D_Creation_Defaults()
        {
            var renderer = new ShapeRenderer2D();
            Assert.Equal(SKColors.White, renderer.Color);
            Assert.Equal(ShapeType.Rectangle, renderer.ShapeType);
            Assert.Equal(new Vector2(100, 100), renderer.Size);
            Assert.True(renderer.Centered);
            Assert.Equal(Vector2.Zero, renderer.Position);
        }

        [Fact]
        public void ShapeRenderer2D_SetProperties()
        {
            var renderer = new ShapeRenderer2D();
            var newColor = SKColors.CornflowerBlue;
            var newShape = ShapeType.Circle;
            var newSize = new Vector2(50, 50);
            var newCentered = false;
            renderer.Color = newColor;
            renderer.ShapeType = newShape;
            renderer.Size = newSize;
            renderer.Centered = newCentered;
            Assert.Equal(newColor, renderer.Color);
            Assert.Equal(newShape, renderer.ShapeType);
            Assert.Equal(newSize, renderer.Size);
            Assert.False(renderer.Centered);
        }

        // --- Drawing Tests (Modified to Save Output) ---

        [Fact]
        public void DrawSelf_Rectangle_Centered_DrawsSomething()
        {
            // Arrange
            var node = new ShapeRenderer2D { Position = new Vector2(50, 50) };
            node.Color = SKColors.Red;
            node.Size = new Vector2(20, 30); // Changed size slightly
            node.Centered = true;
            (var surface, var canvas) = CreateTestCanvas(100, 100);
            canvas.Clear(SKColors.Black);

            // Act
            node._Draw(canvas);
            // Save the output
            SaveCanvasToFile(surface, nameof(DrawSelf_Rectangle_Centered_DrawsSomething));

            // Assert (Optional pixel check)
            using var image = surface.Snapshot();
            using var bitmap = SKBitmap.FromImage(image);
            SKColor centerPixel = bitmap.GetPixel(50, 50); // Center of the node
            Assert.True(centerPixel.Red > 200 && centerPixel.Green < 50 && centerPixel.Blue < 50, "Center pixel should be Red");
        }

        [Fact]
        public void DrawSelf_Rectangle_TopLeft_DrawsSomething()
        {
             // Arrange
            var node = new ShapeRenderer2D { Position = new Vector2(10, 20) };
            node.Color = SKColors.Lime;
            node.Size = new Vector2(30, 40);
            node.Centered = false;
            (var surface, var canvas) = CreateTestCanvas(100, 100);
            canvas.Clear(SKColors.DarkGray); // Changed background

            // Act
            node._Draw(canvas);
             // Save the output
            SaveCanvasToFile(surface, nameof(DrawSelf_Rectangle_TopLeft_DrawsSomething));

            // Assert (Optional pixel checks)
             using var image = surface.Snapshot();
             using var bitmap = SKBitmap.FromImage(image);
             SKColor innerPixel = bitmap.GetPixel(15, 25); // Inside rect area [10,40)x[20,60)
             Assert.True(innerPixel.Red < 50 && innerPixel.Green > 200 && innerPixel.Blue < 50, "Inner pixel should be Lime");
             SKColor outerPixel = bitmap.GetPixel(5, 5); // Outside rect area
             Assert.Equal(SKColors.DarkGray.Red, outerPixel.Red);
             Assert.Equal(SKColors.DarkGray.Green, outerPixel.Green);
             Assert.Equal(SKColors.DarkGray.Blue, outerPixel.Blue);
        }

        [Fact]
         public void DrawSelf_Circle_Centered_DrawsSomething()
         {
             // Arrange
             var node = new ShapeRenderer2D { Position = new Vector2(70, 30) };
             node.ShapeType = ShapeType.Circle;
             node.Color = SKColors.Blue;
             node.Size = new Vector2(40, 40); // Diameter 40 -> Radius 20
             node.Centered = true;
             (var surface, var canvas) = CreateTestCanvas(100, 100);
             canvas.Clear(SKColors.WhiteSmoke); // Changed background

             // Act
             node._Draw(canvas);
             // Save the output
             SaveCanvasToFile(surface, nameof(DrawSelf_Circle_Centered_DrawsSomething));

             // Assert (Optional pixel checks)
             using var image = surface.Snapshot();
             using var bitmap = SKBitmap.FromImage(image);
             SKColor centerPixel = bitmap.GetPixel(70, 30); // Center should be blue
             Assert.True(centerPixel.Red < 50 && centerPixel.Green < 50 && centerPixel.Blue > 200, "Center pixel should be Blue");
             SKColor outerPixel = bitmap.GetPixel(91, 30); // Outside radius (70+20=90)
             Assert.Equal(SKColors.WhiteSmoke.Red, outerPixel.Red);
             Assert.Equal(SKColors.WhiteSmoke.Green, outerPixel.Green);
             Assert.Equal(SKColors.WhiteSmoke.Blue, outerPixel.Blue);
         }

         // TODO: Add test for Circle with Centered = false and save output
         // TODO: Add tests involving Rotation and Scale affecting the drawn shape area and save output
    }
}