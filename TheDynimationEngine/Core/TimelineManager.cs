// File: Core/TimelineManager.cs
using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp; // For SKColor

namespace TheDynimationEngine.Core
{
    /// <summary>
    /// Represents an entry on the timeline, associating a scene's root Node
    /// with a start time and duration.
    /// </summary>
    public class TimelineEntry
    {
        /// <summary>
        /// The root node of the scene associated with this entry.
        /// It's expected that this node (and its children) are fully configured.
        /// </summary>
        public Node SceneRoot { get; }

        /// <summary>
        /// The time (in seconds) when this entry becomes active.
        /// </summary>
        public float StartTime { get; }

        /// <summary>
        /// The time (in seconds) when this entry becomes inactive (exclusive).
        /// </summary>
        public float EndTime { get; }

        /// <summary>
        /// The duration (in seconds) for which this entry is active.
        /// </summary>
        public float Duration => EndTime - StartTime;

        // TODO: Add Layer property for explicit draw ordering?
        // public int Layer { get; } = 0;

        // TODO: Add Name/Tag for identification?
        // public string Name { get; set; } = "TimelineEntry";

        /// <summary>
        /// Creates a new timeline entry.
        /// </summary>
        /// <param name="sceneRoot">The root node of the scene for this entry.</param>
        /// <param name="startTime">The start time (seconds). Must be non-negative.</param>
        /// <param name="duration">The duration (seconds). Must be positive.</param>
        /// <exception cref="ArgumentNullException">Thrown if sceneRoot is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if startTime is negative or duration is non-positive.</exception>
        public TimelineEntry(Node sceneRoot, float startTime, float duration)
        {
            SceneRoot = sceneRoot ?? throw new ArgumentNullException(nameof(sceneRoot));
            if (startTime < 0) throw new ArgumentOutOfRangeException(nameof(startTime), "Start time cannot be negative.");
            if (duration <= 0) throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive.");

            StartTime = startTime;
            EndTime = StartTime + duration;
        }

        /// <summary>
        /// Checks if this timeline entry is active at the given time.
        /// </summary>
        /// <param name="currentTime">The current time in seconds.</param>
        /// <returns>True if active, false otherwise.</returns>
        public bool IsActive(float currentTime)
        {
            // Active during the interval [StartTime, EndTime)
            return currentTime >= StartTime && currentTime < EndTime;
        }
    }


    /// <summary>
    /// Manages a collection of TimelineEntry objects, determining which scenes
    /// are active at any given time and calculating the total animation duration.
    /// Also stores global output settings like resolution and background color.
    /// </summary>
    public class TimelineManager
    {
        private readonly List<TimelineEntry> _entries = new List<TimelineEntry>();
        private bool _isSorted = true; // Assume sorted initially

        /// <summary>
        /// Gets the target width for the output animation frames.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the target height for the output animation frames.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Gets the default background color for animation frames.
        /// </summary>
        public SKColor BackgroundColor { get; }

        /// <summary>
        /// Gets the calculated total duration of the timeline based on the end times
        /// of all added entries.
        /// </summary>
        public float TotalDuration { get; private set; } = 0f;

        /// <summary>
        /// Gets a read-only list of all entries added to the timeline.
        /// </summary>
        public IReadOnlyList<TimelineEntry> Entries => _entries.AsReadOnly();

        /// <summary>
        /// Creates a new TimelineManager instance.
        /// </summary>
        /// <param name="width">Target output width (pixels).</param>
        /// <param name="height">Target output height (pixels).</param>
        /// <param name="backgroundColor">Default background color.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if width or height are non-positive.</exception>
        public TimelineManager(int width, int height, SKColor backgroundColor)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");

            Width = width;
            Height = height;
            BackgroundColor = backgroundColor;
        }

        /// <summary>
        /// Adds a TimelineEntry to the manager.
        /// </summary>
        /// <param name="entry">The entry to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if entry is null.</exception>
        public void AddEntry(TimelineEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            _entries.Add(entry);
            _isSorted = false; // Adding might disrupt sorting order

            // Update total duration
            if (entry.EndTime > TotalDuration)
            {
                TotalDuration = entry.EndTime;
            }
        }

        /// <summary>
        /// Adds a scene root with specified timing directly. Convenience method for AddEntry.
        /// </summary>
        /// <param name="sceneRoot">The root node of the scene.</param>
        /// <param name="startTime">The start time (seconds).</param>
        /// <param name="duration">The duration (seconds).</param>
        public void AddScene(Node sceneRoot, float startTime, float duration)
        {
            AddEntry(new TimelineEntry(sceneRoot, startTime, duration));
        }

        /// <summary>
        /// Gets the root nodes of all timeline entries active at the specified time.
        /// The order returned might influence draw order if entries overlap.
        /// (Currently returns in the order they were added or sorted by start time).
        /// </summary>
        /// <param name="currentTime">The current time in seconds.</param>
        /// <returns>An enumerable collection of active root Nodes.</returns>
        public IEnumerable<Node> GetActiveSceneRoots(float currentTime)
        {
            // Optional: Sort if needed for consistent overlap handling or optimization
            // EnsureSortedByStartTime();

            var activeRoots = new List<Node>();
            foreach (var entry in _entries)
            {
                if (entry.IsActive(currentTime))
                {
                    activeRoots.Add(entry.SceneRoot);
                }
            }
            return activeRoots;

            // Alternative using LINQ (less readable maybe?)
            // return _entries.Where(entry => entry.IsActive(currentTime)).Select(entry => entry.SceneRoot);
        }

        /// <summary>
        /// Ensures the internal list of entries is sorted by start time.
        /// Called internally by methods that might rely on order.
        /// </summary>
        private void EnsureSortedByStartTime()
        {
            if (!_isSorted)
            {
                _entries.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
                _isSorted = true;
            }
        }

        // TODO: Add methods for removing entries, finding entries by name/tag?
        // TODO: Add methods for handling transitions between entries?
    }
}