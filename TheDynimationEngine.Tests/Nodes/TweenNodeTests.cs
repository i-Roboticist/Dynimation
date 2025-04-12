// File: TheDynimationEngine.Tests/Nodes/TweenNodeTests.cs
using Xunit;
using TheDynimationEngine.Core;
using TheDynimationEngine.Nodes;
using TheDynimationEngine.Tweening;
using System.Numerics;
using SkiaSharp;
using Xunit.Abstractions; // For output

namespace TheDynimationEngine.Tests.Nodes
{
    // Simple target class for testing property tweening
    public class TweenTargetNode : Node2D // Inherit Node2D for Position etc.
    {
        public float FloatValue { get; set; } = 0f;
        public Vector2 VectorValue { get; set; } = Vector2.Zero;
        public SKColor ColorValue { get; set; } = SKColors.Black;
        public int ReadOnlyValue { get; } = 5; // Read-only property to test failure
    }


    public class TweenNodeTests
    {
        private readonly ITestOutputHelper _output;

        public TweenNodeTests(ITestOutputHelper output) { _output = output; }

        // Helper to simulate SceneTree processing
        private void SimulateSceneTreeProcess(SceneTree sceneTree, float totalTime, int steps = 10)
        {
            if (steps <= 0) steps = 1;
            float delta = totalTime / steps;
            for (int i = 0; i < steps; i++) { sceneTree.ProcessFrame(delta); }
        }

        // Helper for float asserts
        private void AssertFloatEqual(float expected, float actual, float tolerance = 1e-5f)
        { Assert.True(Math.Abs(expected - actual) < tolerance, $"Expected: {expected}, Actual: {actual}"); }

        // Helper for Vector2 asserts
        private void AssertVectorEqual(Vector2 expected, Vector2 actual, float tolerance = 1e-5f)
        { Assert.True(Vector2.DistanceSquared(expected, actual) < tolerance * tolerance, $"Expected: {expected}, Actual: {actual}"); }

        // --- Tests ---

        [Fact]
        public void TweenNode_FloatProperty_Linear()
        {
            var target = new TweenTargetNode { FloatValue = 10f };
            var tween = new TweenNode();
            var root = new Node(); var sceneTree = new SceneTree(root); root.AddChild(tween);
            tween.TweenProperty(target, nameof(TweenTargetNode.FloatValue), 110f, 1.0f, 0f, Easing.Linear);
            tween.Start();
            SimulateSceneTreeProcess(sceneTree, 0.5f);
            AssertFloatEqual(60f, target.FloatValue);
            SimulateSceneTreeProcess(sceneTree, 0.5f);
            AssertFloatEqual(110f, target.FloatValue);
            Assert.Null(tween.Parent); Assert.Null(tween.SceneTree);
        }

        [Fact]
        public void TweenNode_Vector2Property_EaseOutQuad()
        {
            var target = new TweenTargetNode { VectorValue = Vector2.Zero };
            var tween = new TweenNode();
            var root = new Node(); var sceneTree = new SceneTree(root); root.AddChild(tween);
            Vector2 endValue = new Vector2(100, -50);
            tween.TweenProperty(target, nameof(TweenTargetNode.VectorValue), endValue, 2.0f, 0f, Easing.EaseOutQuad);
            tween.Start();
            SimulateSceneTreeProcess(sceneTree, 1.0f);
            float t = 0.5f; float easedT = Easing.EaseOutQuad(t); Vector2 expectedMid = Vector2.Lerp(Vector2.Zero, endValue, easedT);
            AssertVectorEqual(expectedMid, target.VectorValue);
            SimulateSceneTreeProcess(sceneTree, 1.0f);
            AssertVectorEqual(endValue, target.VectorValue); Assert.Null(tween.Parent);
        }

        [Fact]
        public void TweenNode_ColorProperty_Linear()
        {
            var target = new TweenTargetNode { ColorValue = SKColors.Red };
            var tween = new TweenNode();
            var root = new Node(); var sceneTree = new SceneTree(root); root.AddChild(tween);
            SKColor endValue = SKColors.Blue;
            tween.TweenProperty(target, nameof(TweenTargetNode.ColorValue), endValue, 1.0f, 0f, Easing.Linear);
            tween.Start();
            SimulateSceneTreeProcess(sceneTree, 0.5f);
            _output.WriteLine($"Midpoint Color: R={target.ColorValue.Red} G={target.ColorValue.Green} B={target.ColorValue.Blue} A={target.ColorValue.Alpha}");
            Assert.InRange(target.ColorValue.Red, (byte)127, (byte)128); Assert.Equal(0, target.ColorValue.Green); Assert.InRange(target.ColorValue.Blue, (byte)127, (byte)128); Assert.Equal(255, target.ColorValue.Alpha);
            SimulateSceneTreeProcess(sceneTree, 0.5f);
            Assert.Equal(endValue.Red, target.ColorValue.Red); Assert.Equal(endValue.Green, target.ColorValue.Green); Assert.Equal(endValue.Blue, target.ColorValue.Blue); Assert.Equal(endValue.Alpha, target.ColorValue.Alpha);
            Assert.Null(tween.Parent);
        }

        [Fact]
        public void TweenNode_WithDelay()
        {
            var target = new TweenTargetNode { FloatValue = 0f };
            var tween = new TweenNode();
            var root = new Node(); var sceneTree = new SceneTree(root); root.AddChild(tween);
            tween.TweenProperty(target, nameof(TweenTargetNode.FloatValue), 100f, 1.0f, 0.5f, Easing.Linear);
            tween.Start();

            SimulateSceneTreeProcess(sceneTree, 0.25f); // t = 0.25 (in delay)
            AssertFloatEqual(0f, target.FloatValue); // Should still be start value

            SimulateSceneTreeProcess(sceneTree, 0.50f); // t = 0.75 (0.25 into tween)
            AssertFloatEqual(25f, target.FloatValue); // Should be 25% done

            // --- MODIFIED SIMULATION TIME ---
            // Simulate slightly longer than needed to ensure the final frame processing
            // where the tween finishes AND QueueFree is processed definitely happens.
            // Old time: 0.75f (total 1.50s)
            // New time: 0.75f + small extra (e.g., 0.01f, or just make it 0.8f total)
            SimulateSceneTreeProcess(sceneTree, 0.8f); // t = 1.55 (well past end)

            // Assert (End)
            AssertFloatEqual(100f, target.FloatValue); // Should have reached end value
            Assert.Null(tween.Parent); // QueueFree should have been processed
        }

        [Fact]
        public void TweenNode_MultipleProperties()
        {
            var target = new TweenTargetNode { FloatValue = 0f, Position = Vector2.Zero };
            var tween = new TweenNode();
            var root = new Node(); var sceneTree = new SceneTree(root); root.AddChild(tween);
            Vector2 endPos = new Vector2(50, 50);
            tween.TweenProperty(target, nameof(TweenTargetNode.FloatValue), 10f, 1.0f)
                 .TweenProperty(target, nameof(TweenTargetNode.Position), endPos, 2.0f);
            tween.Start();
            SimulateSceneTreeProcess(sceneTree, 1.0f);
            AssertFloatEqual(10f, target.FloatValue); AssertVectorEqual(new Vector2(25, 25), target.Position);
            SimulateSceneTreeProcess(sceneTree, 1.0f);
            AssertFloatEqual(10f, target.FloatValue); AssertVectorEqual(endPos, target.Position);
            Assert.Null(tween.Parent);
        }

        [Fact]
        public void TweenNode_InvalidProperty_Throws()
        {
            var target = new TweenTargetNode();
            var tween = new TweenNode();
            var root = new Node(); var sceneTree = new SceneTree(root); root.AddChild(tween);
            Assert.Throws<ArgumentException>(() => tween.TweenProperty(target, "NonExistentProperty", 10f, 1.0f));
            Assert.Throws<ArgumentException>(() => tween.TweenProperty(target, nameof(TweenTargetNode.ReadOnlyValue), 10, 1.0f));
        }

        [Fact]
        public void TweenNode_StartWhileRunning_LogsWarning()
        {
            var target = new TweenTargetNode();
            var tween = new TweenNode();
            var root = new Node(); var sceneTree = new SceneTree(root); root.AddChild(tween);
            tween.TweenProperty(target, nameof(TweenTargetNode.FloatValue), 10f, 1.0f);
            tween.Start();
            tween.Start(); // Call start again
            Assert.True(true);
        }

        [Fact]
        public void TweenNode_AutoDeleteFalse()
        {
            var target = new TweenTargetNode { FloatValue = 0f };
            var tween = new TweenNode();
            var root = new Node(); var sceneTree = new SceneTree(root); root.AddChild(tween);
            tween.TweenProperty(target, nameof(TweenTargetNode.FloatValue), 100f, 0.5f).SetAutoDelete(false);
            tween.Start();
            SimulateSceneTreeProcess(sceneTree, 1.0f);
            AssertFloatEqual(100f, target.FloatValue);
            Assert.NotNull(tween.Parent);
            Assert.Same(root, tween.Parent);
        }
    }
}