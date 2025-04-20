// File: TheDynimationEngine.Tests/Core/TimelineManagerTests.cs
using Xunit;
using TheDynimationEngine.Core;
using TheDynimationEngine.Nodes; // For Node
using SkiaSharp;
using System.Linq; // For Linq Count() etc.
using System; // For ArgumentOutOfRangeException etc.
using System.Collections.Generic; // For List<> in GetActiveSceneRoots test

namespace TheDynimationEngine.Tests.Core
{
    public class TimelineManagerTests
    {
        // Helper Nodes for testing
        private Node CreateDummyNode(string name) => new Node { Name = name };

        [Fact]
        public void TimelineManager_Creation_SetsProperties()
        {
            // Arrange
            int width = 1920;
            int height = 1080;
            SKColor bg = SKColors.CornflowerBlue;

            // Act
            var tm = new TimelineManager(width, height, bg);

            // Assert
            Assert.Equal(width, tm.Width);
            Assert.Equal(height, tm.Height);
            Assert.Equal(bg, tm.BackgroundColor);
            Assert.Equal(0f, tm.TotalDuration); // Starts at 0
            Assert.Empty(tm.Entries);          // Starts empty
        }

        [Theory]
        [InlineData(0, 10)] // Invalid width
        [InlineData(10, 0)] // Invalid height
        [InlineData(-1, 10)]
        [InlineData(10, -1)]
        public void TimelineManager_Creation_ThrowsOnInvalidDimensions(int w, int h)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TimelineManager(w, h, SKColors.White));
        }

        [Fact]
        public void TimelineEntry_Creation_SetsPropertiesAndValidates()
        {
            // Arrange
            var node = CreateDummyNode("Test");
            float start = 1.5f;
            float duration = 2.0f;

            // Act
            var entry = new TimelineEntry(node, start, duration);

            // Assert
            Assert.Same(node, entry.SceneRoot);
            Assert.Equal(start, entry.StartTime);
            Assert.Equal(duration, entry.Duration);
            Assert.Equal(start + duration, entry.EndTime);

            // Test validation
            Assert.Throws<ArgumentNullException>(() => new TimelineEntry(null!, 0f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TimelineEntry(node, -0.1f, 1f)); // Negative start
            Assert.Throws<ArgumentOutOfRangeException>(() => new TimelineEntry(node, 0f, 0f));   // Zero duration
            Assert.Throws<ArgumentOutOfRangeException>(() => new TimelineEntry(node, 0f, -1f));  // Negative duration
        }

        [Theory]
        // Args: startTime, duration, currentTime, expectedIsActive
        [InlineData(0f, 2f, 1f, true)]    // Within interval
        [InlineData(0f, 2f, 0f, true)]    // Exactly at start
        [InlineData(0f, 2f, 1.999f, true)] // Just before end
        [InlineData(0f, 2f, -0.1f, false)] // Before start
        [InlineData(0f, 2f, 2f, false)]   // Exactly at end (exclusive)
        [InlineData(0f, 2f, 2.1f, false)]  // After end
        [InlineData(1f, 3f, 2.5f, true)]  // Different start time, within
        [InlineData(1f, 3f, 0.5f, false)] // Different start time, before
        [InlineData(1f, 3f, 4.0f, false)]  // Different start time, exactly at end
        [InlineData(1f, 3f, 4.1f, false)] // Different start time, after
        // --- The Failing Case ---
        [InlineData(0f, 1f, 2f, false)]  // CORRECTED: CurrentTime=2 is AFTER EndTime=1, should be false
        // --- Previous Incorrect Case ---
        // [InlineData(0f, 1f, 2f, true)]
        public void TimelineEntry_IsActive_ReturnsCorrectValue(float start, float duration, float currentTime, bool expected)
        {
            // Arrange
            var entry = new TimelineEntry(CreateDummyNode("Test"), start, duration);

            // Act
            bool actual = entry.IsActive(currentTime);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TimelineManager_AddEntry_AddsToListAndUpdatesDuration()
        {
            // Arrange
            var tm = new TimelineManager(100, 100, SKColors.White);
            var node1 = CreateDummyNode("N1");
            var node2 = CreateDummyNode("N2");
            var entry1 = new TimelineEntry(node1, 0f, 2.0f); // Ends at 2.0
            var entry2 = new TimelineEntry(node2, 1.5f, 1.0f); // Ends at 2.5

            // Act
            tm.AddEntry(entry1);
            tm.AddEntry(entry2);

            // Assert
            Assert.Equal(2, tm.Entries.Count);
            Assert.Contains(entry1, tm.Entries);
            Assert.Contains(entry2, tm.Entries);
            Assert.Equal(2.5f, tm.TotalDuration); // Should be the latest end time

            // Add another entry ending earlier
            var entry3 = new TimelineEntry(CreateDummyNode("N3"), 0.5f, 1.0f); // Ends at 1.5
            tm.AddEntry(entry3);
            Assert.Equal(3, tm.Entries.Count);
            Assert.Equal(2.5f, tm.TotalDuration); // Duration should not decrease
        }

         [Fact]
        public void TimelineManager_AddScene_AddsEntryCorrectly()
        {
             // Arrange
            var tm = new TimelineManager(100, 100, SKColors.White);
            var node = CreateDummyNode("N");

            // Act
            tm.AddScene(node, 1f, 3f); // Use convenience method

            // Assert
            Assert.Single(tm.Entries);
            var entry = tm.Entries[0];
            Assert.Same(node, entry.SceneRoot);
            Assert.Equal(1f, entry.StartTime);
            Assert.Equal(3f, entry.Duration);
            Assert.Equal(4f, entry.EndTime);
            Assert.Equal(4f, tm.TotalDuration);
        }


        [Fact]
        public void TimelineManager_GetActiveSceneRoots_ReturnsCorrectNodes()
        {
            // Arrange
            var tm = new TimelineManager(100, 100, SKColors.White);
            var nodeA = CreateDummyNode("A"); // 0 - 2
            var nodeB = CreateDummyNode("B"); // 1 - 3
            var nodeC = CreateDummyNode("C"); // 2.5 - 4
            var nodeD = CreateDummyNode("D"); // 5 - 6 (inactive at tested times)

            tm.AddScene(nodeA, 0f, 2f);
            tm.AddScene(nodeB, 1f, 2f);
            tm.AddScene(nodeC, 2.5f, 1.5f);
            tm.AddScene(nodeD, 5f, 1f);

            // Act & Assert
            var active_t0_5 = tm.GetActiveSceneRoots(0.5f).ToList();
            Assert.Single(active_t0_5); Assert.Same(nodeA, active_t0_5[0]);

            var active_t1_5 = tm.GetActiveSceneRoots(1.5f).ToList();
            Assert.Equal(2, active_t1_5.Count); Assert.Contains(nodeA, active_t1_5); Assert.Contains(nodeB, active_t1_5);

            var active_t2_2 = tm.GetActiveSceneRoots(2.2f).ToList();
            Assert.Single(active_t2_2); Assert.Same(nodeB, active_t2_2[0]);

            var active_t3_0 = tm.GetActiveSceneRoots(3.0f).ToList();
            Assert.Single(active_t3_0); Assert.Same(nodeC, active_t3_0[0]);

            var active_t4_5 = tm.GetActiveSceneRoots(4.5f).ToList();
            Assert.Empty(active_t4_5);

            var active_t5_5 = tm.GetActiveSceneRoots(5.5f).ToList();
            Assert.Single(active_t5_5); Assert.Same(nodeD, active_t5_5[0]);
        }
    }
}