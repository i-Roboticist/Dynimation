// File: Rendering/FrameSequenceExporter.cs
using System;
using System.Collections.Generic;
using System.IO;
using TheDynimationEngine.Core; // Needs SceneTree, TimelineManager (needs definition/refinement)
using SkiaSharp;
using System.Linq; // Added for GetActiveSceneRoots().ToList()

namespace TheDynimationEngine.Rendering
{
    // --- Refined TimelineManager Concept (Placeholder - Needs Full Implementation Later) ---
    public class TimelineEntry
    {
        public Node SceneRoot { get; }
        public float StartTime { get; }
        public float EndTime { get; }
        public float Duration => EndTime - StartTime;
        public TimelineEntry(Node sceneRoot, float startTime, float duration)
        {
            SceneRoot = sceneRoot ?? throw new ArgumentNullException(nameof(sceneRoot));
            StartTime = Math.Max(0f, startTime);
            if (duration <= 0) throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive.");
            EndTime = StartTime + duration;
        }
        public bool IsActive(float currentTime) => currentTime >= StartTime && currentTime < EndTime;
    }

    public class TimelineManager // Replace previous basic definition if exists in Core
    {
        private readonly List<TimelineEntry> _entries = new List<TimelineEntry>();
        public int Width { get; }
        public int Height { get; }
        public SKColor BackgroundColor { get; }
        public float TotalDuration { get; private set; } = 0f;

        public TimelineManager(int width, int height, SKColor backgroundColor)
        {
            Width = width > 0 ? width : 1; Height = height > 0 ? height : 1; BackgroundColor = backgroundColor;
        }
        public void AddEntry(TimelineEntry entry)
        {
            _entries.Add(entry); if (entry.EndTime > TotalDuration) TotalDuration = entry.EndTime;
        }
        public IEnumerable<Node> GetActiveSceneRoots(float currentTime)
        {
             var activeRoots = new List<Node>();
             foreach(var entry in _entries) { if(entry.IsActive(currentTime)) activeRoots.Add(entry.SceneRoot); }
             return activeRoots;
        }
    }

    // --- The Actual Exporter ---
    public class FrameSequenceExporter
    {
        private readonly TimelineManager _timelineManager;
        private readonly int _frameRate;
        private readonly string _outputDirectory;
        private readonly string _fileNamePrefix;
        private readonly SKEncodedImageFormat _imageFormat;
        private readonly int _quality;

        public FrameSequenceExporter(
            TimelineManager timelineManager, int frameRate, string outputDirectory,
            string fileNamePrefix = "frame_", SKEncodedImageFormat format = SKEncodedImageFormat.Png, int quality = 95)
        {
            _timelineManager = timelineManager ?? throw new ArgumentNullException(nameof(timelineManager));
            if (frameRate <= 0) throw new ArgumentOutOfRangeException(nameof(frameRate), "Frame rate must be positive.");
            _outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
            _fileNamePrefix = fileNamePrefix ?? "frame_";
            if (quality < 0 || quality > 100) throw new ArgumentOutOfRangeException(nameof(quality), "Quality must be between 0 and 100.");
            _frameRate = frameRate; _imageFormat = format; _quality = quality;
        }

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

            // --- CORRECTED: Create directory ONLY if frames will be generated ---
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
                float deltaTime = timeStep;
                try
                {
                     var activeRoots = _timelineManager.GetActiveSceneRoots(currentTime).ToList();
                     using var surface = SKSurface.Create(imageInfo);
                     if (surface == null) { Console.WriteLine($"\nError: Could not create drawing surface for frame {frame}. Skipping."); continue; } // Continue instead of break?

                     SKCanvas canvas = surface.Canvas;
                     canvas.Clear(_timelineManager.BackgroundColor);

                     foreach (var rootNode in activeRoots)
                     {
                         // This SceneTree management needs refinement later
                         SceneTree tempTree = rootNode.SceneTree ?? new SceneTree(rootNode);
                         tempTree.ProcessFrame(deltaTime);
                         tempTree.DrawFrame(canvas);
                         // How to handle if tempTree was created? rootNode.SceneTree = null?
                     }

                     string fileExtension = _imageFormat.ToString().ToLowerInvariant();
                     string frameFileName = $"{_fileNamePrefix}{frame:D5}.{fileExtension}";
                     string frameOutputPath = Path.Combine(_outputDirectory, frameFileName);

                     using (SKImage renderedImage = surface.Snapshot())
                     using (SKData encodedData = renderedImage.Encode(_imageFormat, _quality))
                     {
                         if (encodedData == null) { Console.WriteLine($"\nError: Failed to encode frame {frame}."); continue; }
                         using (var stream = File.OpenWrite(frameOutputPath)) { encodedData.SaveTo(stream); }
                     }

                     if ((frame + 1) % _frameRate == 0 || frame == totalFrames - 1 || frame == 0)
                     {
                         float progressPercent = (float)(frame + 1) / totalFrames * 100f;
                         Console.Write($"\rProgress: {frame + 1}/{totalFrames} ({progressPercent:F1}%) - Time: {currentTime:F2}s");
                         if(frame == totalFrames - 1) Console.Write("          \n");
                     }
                }
                catch (Exception ex)
                {
                     Console.WriteLine($"\n\n*** Unhandled error rendering frame {frame} (Time: {currentTime:F2}s) ***");
                     Console.WriteLine(ex.ToString());
                     Console.WriteLine("*** Stopping export. ***");
                     break;
                }
            } // End frame loop
            Console.WriteLine($"-------------------------------------");
            Console.WriteLine($"Finished exporting frames to '{_outputDirectory}'."); // Log actual dir used
        }
    }
}