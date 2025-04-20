// File: TheDynimationEngine.Tests/IO/AssetLoaderTests.cs
using Xunit;
using TheDynimationEngine.IO; // Use AssetLoader
using TheDynimationEngine.Rendering; // Use Texture
using SkiaSharp;
using System.IO;
using Xunit.Abstractions;
using System;

namespace TheDynimationEngine.Tests.IO
{
    public class AssetLoaderTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _testAssetsDir;
        private readonly string _validImagePath;
        private readonly string _invalidPath;
        private readonly string _notImagePath;

        // Setup: Create test asset directory and files
        public AssetLoaderTests(ITestOutputHelper output)
        {
            _output = output;
            _testAssetsDir = Path.Combine(Path.GetTempPath(), $"DynimationLoaderTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testAssetsDir);
            _output.WriteLine($"Test asset dir: {_testAssetsDir}");

            // 1. Create a valid image file
            _validImagePath = Path.Combine(_testAssetsDir, "valid_image.png");
            try
            {
                var info = new SKImageInfo(5, 5, SKColorType.Rgba8888);
                using var surface = SKSurface.Create(info);
                if(surface == null) throw new InvalidOperationException("Cannot create surface for valid image.");
                surface.Canvas.Clear(SKColors.Green);
                using var img = surface.Snapshot(); using var data = img.Encode(SKEncodedImageFormat.Png, 100); using var fs = File.OpenWrite(_validImagePath); data.SaveTo(fs);
                _output.WriteLine($"Created valid image: {_validImagePath}");
            }
            catch(Exception ex) { _output.WriteLine($"ERROR creating valid image: {ex.Message}"); throw; }

            // 2. Define path for a non-existent file
            _invalidPath = Path.Combine(_testAssetsDir, "i_dont_exist.png");

            // 3. Create a non-image file
            _notImagePath = Path.Combine(_testAssetsDir, "not_an_image.txt");
            try
            {
                 File.WriteAllText(_notImagePath, "Hello world");
                 _output.WriteLine($"Created invalid file: {_notImagePath}");
            }
             catch(Exception ex) { _output.WriteLine($"ERROR creating invalid file: {ex.Message}"); throw; }
        }

        // Cleanup
        public void Dispose()
        {
            try { if (Directory.Exists(_testAssetsDir)) { Directory.Delete(_testAssetsDir, true); _output.WriteLine($"Cleaned up test assets dir: {_testAssetsDir}"); } }
            catch (Exception ex) { _output.WriteLine($"Warning: Cleanup failed: {ex.Message}"); }
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void AssetLoader_Load_Texture_Success()
        {
            // Arrange
            Texture? loadedTexture = null;

            // Act
            try
            {
                loadedTexture = AssetLoader.Load<Texture>(_validImagePath);

                // Assert
                Assert.NotNull(loadedTexture);
                Assert.True(loadedTexture.IsValid);
                Assert.Equal(5, loadedTexture.Width);
                Assert.Equal(5, loadedTexture.Height);
            }
            finally
            {
                 loadedTexture?.Dispose(); // Clean up loaded texture
            }
        }

        [Fact]
        public void AssetLoader_Load_Texture_NotFound()
        {
            // Arrange & Act
            var loadedTexture = AssetLoader.Load<Texture>(_invalidPath);

            // Assert
            Assert.Null(loadedTexture);
        }

        [Fact]
        public void AssetLoader_Load_Texture_InvalidFormat()
        {
            // Arrange & Act
            var loadedTexture = AssetLoader.Load<Texture>(_notImagePath);

            // Assert
            Assert.Null(loadedTexture); // Texture.LoadFromFile should handle decode failure
        }

        [Fact]
        public void AssetLoader_Load_UnsupportedType_ReturnsNull()
        {
            // Arrange - Use a type the loader doesn't support (e.g., Stream)
            // Act
            var loadedStream = AssetLoader.Load<MemoryStream>(_validImagePath); // Try loading image path as stream

            // Assert
            Assert.Null(loadedStream);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void AssetLoader_Load_InvalidPath_ReturnsNull(string? path)
        {
             // Arrange & Act
             var loadedTexture = AssetLoader.Load<Texture>(path!); // Use ! to satisfy nullable analysis for test case

             // Assert
             Assert.Null(loadedTexture);
        }
    }
}