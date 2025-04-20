// File: Rendering/FrameSequenceExporter.cs
using System;
using System.Collections.Generic;
using System.IO;
using TheDynimationEngine.Core; // Added: Using the proper Core classes
using SkiaSharp;
using System.Linq; // For ToList()

namespace TheDynimationEngine.Rendering
{
    // --- Placeholders Removed ---
    // The TimelineEntry and TimelineManager definitions that were previously here
    // have been removed, as they are now correctly defined in Core/TimelineManager.cs

    /// <summary>
    /// Exports an animation defined by a TimelineManager to a sequence of image frames.
    /// </summary>
    public class FrameSequenceExporter
    {
        private readonly TimelineManager _timelineManager;
        private readonly int _frameRate;
        private readonly string _outputDirectory;
        private readonly string _fileNamePrefix;
        private readonly SKEncodedImageFormat _imageFormat;
        private readonly int _quality; // For JPEG/WebP

        /// <summary>
        /// Creates a new exporter instance.
        /// </summary>
        /// <param name="timelineManager">The timeline defining the animation.</param>
        /// <param name="frameRate">Output frame rate (frames per second).</param>
        /// <param name="outputDirectory">Directory to save the frames.</param>
        /// <param name="fileNamePrefix">Prefix for output filenames (e.g., "frame_").</param>
        /// <param name="format">Image format for output files.</param>
        /// <param name="quality">Quality setting (1-100) for lossy formats like JPEG.</param>
        public FrameSequenceExporter(
            TimelineManager timelineManager,
            int frameRate,
            string outputDirectory,
            string fileNamePrefix = "frame_",
            SKEncodedImageFormat format = SKEncodedImageFormat.Png,
            int quality = 95)
        {
            _timelineManager = timelineManager ?? throw new ArgumentNullException(nameof(timelineManager));
            if (frameRate <= 0) throw new ArgumentOutOfRangeException(nameof(frameRate), "Frame rate must be positive.");
            _outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
            _fileNamePrefix = fileNamePrefix ?? "frame_"; // Ensure not null
            if (quality < 0 || quality > 100) throw new ArgumentOutOfRangeException(nameof(quality), "Quality must be between 0 and 100.");

            _frameRate = frameRate;
            _imageFormat = format;
            _quality = quality;
        }

        /// <summary>
        /// Renders the entire timeline to an image sequence.
        /// </summary>
        public void Render()
        {
            Console.WriteLine($"Starting frame export...");
            Console.WriteLine($"  Resolution: {_timelineManager.Width}x{_timelineManager.Height}");
            Console.WriteLine($"  Frame Rate: {_frameRate} fps");
            Console.WriteLine($"  Output Dir: {_outputDirectory}"); // Log intended dir
            Console.WriteLine($"  Format: {_imageFormat}");

            float timeStep = 1.0f / _frameRate;
            float totalDuration = _timelineManager.TotalDuration;
            int totalFrames = (int)Math.Ceiling(totalDuration * _frameRate);

            if (totalFrames <= 0)
            {
                Console.WriteLine("Warning: Animation has zero duration. No frames generated.");
                return; // Exit before creating directory or looping
            }

            // Create directory ONLY if frames will be generated
            try
            {
                Directory.CreateDirectory(_outputDirectory);
                Console.WriteLine($"  Output Dir Created: {Path.GetFullPath(_outputDirectory)}");
            }
            catch (Exception ex)
            {
                 Console.WriteLine($"\nError: Could not create output directory '{_outputDirectory}': {ex.Message}. Stopping export.");
                 return;
            }

            Console.WriteLine($"  Total Duration: {totalDuration:F2}s");
            Console.WriteLine($"  Total Frames: {totalFrames}");
            Console.WriteLine($"-------------------------------------");

            var imageInfo = new SKImageInfo(_timelineManager.Width, _timelineManager.Height, SKColorType.Rgba8888, SKAlphaType.Premul);

            for (int frame = 0; frame < totalFrames; frame++)
            {
                float currentTime = frame * timeStep;
                float deltaTime = timeStep; // Constant delta for offline rendering

                // --- Per-Frame Logic ---
                try
                {
                     // 1. Get active scene roots for this time from TimelineManager
                     // Now uses the real TimelineManager from Core namespace
                     var activeRoots = _timelineManager.GetActiveSceneRoots(currentTime).ToList();

                     // Create the surface & canvas for this frame
                     using var surface = SKSurface.Create(imageInfo);
                     if (surface == null)
                     {
                          Console.WriteLine($"\nError: Could not create drawing surface for frame {frame}. Skipping.");
                          continue; // Skip this frame, try next
                     }
                     SKCanvas canvas = surface.Canvas;
                     canvas.Clear(_timelineManager.BackgroundColor);

                     // 2. Process and Draw each active scene
                     foreach (var rootNode in activeRoots)
                     {
                         // This SceneTree management assumes the user provides a root node
                         // that is either already in a SceneTree OR we implicitly create one
                         // per frame for processing. The latter is simpler for now but less efficient
                         // and loses SceneTree state between frames if the same root is used consecutively.
                         // A better approach might be for TimelineManager to manage SceneTree instances,
                         // but let's stick to this simpler model for the moment.
                         SceneTree tempTree = rootNode.SceneTree ?? new SceneTree(rootNode);

                         // Process the logic for this scene tree
                         tempTree.ProcessFrame(deltaTime);

                         // Draw the scene tree
                         tempTree.DrawFrame(canvas);

                         // If a temp tree was created, should we null the node's ref?
                         // This dependency flow needs careful consideration later.
                         // If the user *always* constructs a SceneTree and passes Root, this isn't needed.
                         // if(rootNode.SceneTree == null) { /* What to do? Maybe nothing for now */ }
                     }

                     // 3. Save the frame
                     string fileExtension = _imageFormat.ToString().ToLowerInvariant();
                     string frameFileName = $"{_fileNamePrefix}{frame:D5}.{fileExtension}";
                     string frameOutputPath = Path.Combine(_outputDirectory, frameFileName);

                     using (SKImage renderedImage = surface.Snapshot()) // Use SKImage
                     using (SKData encodedData = renderedImage.Encode(_imageFormat, _quality))
                     {
                         if (encodedData == null)
                         {
                             Console.WriteLine($"\nError: Failed to encode frame {frame} to {_imageFormat}.");
                             continue; // Skip saving this frame
                         }
                         using (var stream = File.OpenWrite(frameOutputPath))
                         {
                             encodedData.SaveTo(stream);
                         }
                     }

                     // Optional: Progress Report
                     if ((frame + 1) % _frameRate == 0 || frame == totalFrames - 1 || frame == 0)
                     {
                         float progressPercent = (float)(frame + 1) / totalFrames * 100f;
                         Console.Write($"\rProgress: {frame + 1}/{totalFrames} ({progressPercent:F1}%) - Time: {currentTime:F2}s");
                         if(frame == totalFrames - 1) Console.Write("          \n"); // Clear line end
                     }
                }
                catch (Exception ex)
                {
                     Console.WriteLine($"\n\n*** Unhandled error rendering frame {frame} (Time: {currentTime:F2}s) ***");
                     Console.WriteLine(ex.ToString());
                     Console.WriteLine("*** Stopping export. ***");
                     break; // Stop export on error
                }
            } // End frame loop

            Console.WriteLine($"-------------------------------------");
            Console.WriteLine($"Finished exporting frames to '{_outputDirectory}'.");
        }
    }
}