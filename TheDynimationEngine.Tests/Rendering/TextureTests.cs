// File: TheDynimationEngine.Tests/Rendering/TextureTests.cs
using Xunit;
using TheDynimationEngine.Rendering; // Use Texture
using SkiaSharp;
using System.IO;
using Xunit.Abstractions; // For output
using System; // For IDisposable and Exception

namespace TheDynimationEngine.Tests.Rendering
{
    public class TextureTests : IDisposable // Implement IDisposable for cleanup
    {
        private readonly ITestOutputHelper _output;
        private readonly string _testAssetsDir = "TestAssets_Texture";
        private readonly string _tempImagePath;

        // Setup: Create a dummy image file for loading tests
        public TextureTests(ITestOutputHelper output)
        {
            _output = output;
            // Use a unique subfolder per test run potentially, or ensure clean deletes
            _testAssetsDir = Path.Combine(Path.GetTempPath(), $"DynimationTestAssets_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testAssetsDir);
            _tempImagePath = Path.Combine(_testAssetsDir, "test_image.png");

            try
            {
                var info = new SKImageInfo(10, 10, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var surface = SKSurface.Create(info);
                if (surface == null) throw new InvalidOperationException("Setup failed: Cannot create surface.");
                surface.Canvas.Clear(SKColors.Red);
                using var image = surface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = File.OpenWrite(_tempImagePath);
                data.SaveTo(stream);
                _output.WriteLine($"Created test image: {_tempImagePath}");
            }
            catch (Exception ex)
            {
                 _output.WriteLine($"ERROR during test setup: {ex.Message}");
                 // Rethrow or handle depending on desired test behavior on setup fail
                 throw;
            }
        }

        // Cleanup: Delete the dummy image file and directory after tests run
        public void Dispose()
        {
            try
            {
                 if (Directory.Exists(_testAssetsDir))
                 {
                      Directory.Delete(_testAssetsDir, true); // Delete directory and its contents
                      _output.WriteLine($"Cleaned up test assets directory: {_testAssetsDir}");
                 }
            }
            catch (Exception ex)
            {
                 _output.WriteLine($"Warning: Error during test cleanup: {ex.Message}");
            }
             GC.SuppressFinalize(this); // Prevent finalizer call if Dispose runs
        }

        [Fact]
        public void Texture_LoadFromFile_Success()
        {
            Texture? tex = null; // Declare outside try for finally block
            try
            {
                tex = Texture.LoadFromFile(_tempImagePath);
                Assert.NotNull(tex); // Assert tex is not null before using it
                Assert.True(tex.IsValid);
                Assert.NotNull(tex.Bitmap); // Assert Bitmap is not null
                Assert.Equal(10, tex.Width);
                Assert.Equal(10, tex.Height);
                Assert.Equal(new System.Numerics.Vector2(10, 10), tex.Size);
                SKColor pixel = tex.Bitmap.GetPixel(5, 5); // Safe to call GetPixel now
                Assert.Equal(SKColors.Red.Red, pixel.Red);
                Assert.Equal(SKColors.Red.Green, pixel.Green);
                Assert.Equal(SKColors.Red.Blue, pixel.Blue);
            }
            finally
            {
                tex?.Dispose(); // Ensure disposal
            }
        }

        [Fact]
        public void Texture_LoadFromFile_NotFound()
        {
            string badPath = Path.Combine(_testAssetsDir, "nonexistent.png");
            var tex = Texture.LoadFromFile(badPath);
            Assert.Null(tex);
        }

        [Fact]
        public void Texture_LoadFromFile_InvalidFormat()
        {
            string invalidFile = Path.Combine(_testAssetsDir, "not_an_image.txt");
            File.WriteAllText(invalidFile, "This is not image data");
            var tex = Texture.LoadFromFile(invalidFile);
            Assert.Null(tex);
        }

        [Fact]
        public void Texture_CreatePlaceholder_Success()
        {
             int width = 5;
             int height = 8;
             SKColor color = SKColors.LimeGreen;
             using var tex = Texture.CreatePlaceholder(width, height, color); // Use using for auto-dispose
             Assert.NotNull(tex);
             Assert.True(tex.IsValid);
             Assert.NotNull(tex.Bitmap);
             Assert.Equal(width, tex.Width);
             Assert.Equal(height, tex.Height);
             SKColor pixel = tex.Bitmap.GetPixel(width / 2, height / 2);
             Assert.Equal(color.Red, pixel.Red);
             Assert.Equal(color.Green, pixel.Green);
             Assert.Equal(color.Blue, pixel.Blue);
             Assert.Equal(color.Alpha, pixel.Alpha);
        }

        [Fact]
        public void Texture_Dispose_ReleasesBitmapAndInvalidates() // Renamed test
        {
            // Arrange
            var tex = Texture.LoadFromFile(_tempImagePath);
            Assert.NotNull(tex);
            Assert.True(tex.IsValid);
            Assert.NotNull(tex.Bitmap); // Verify it's loaded

            // Act
            tex.Dispose();

            // Assert
            Assert.False(tex.IsValid); // IsValid should become false
            Assert.Null(tex.Bitmap);   // The public Bitmap property should now return null

            // DO NOT attempt to use the original bitmap reference after Dispose,
            // as it causes native crashes. Testing the public state is sufficient.
            // Assert.Throws<ObjectDisposedException>(() => bitmap.GetPixel(0,0)); // REMOVED THIS LINE
        }
    }
}