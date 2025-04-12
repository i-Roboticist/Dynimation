// File: TheDynimationEngine.Tests/Tweening/EasingTests.cs
using Xunit;
using TheDynimationEngine.Tweening; // Use the Easing class
using System;

namespace TheDynimationEngine.Tests.Tweening
{
    public class EasingTests
    {
        // Helper for comparing floats with tolerance
        private void AssertFloatEqual(float expected, float actual, float tolerance = 1e-6f)
        {
            Assert.True(Math.Abs(expected - actual) < tolerance, $"Expected: {expected}, Actual: {actual}");
        }

        // --- Test Boundary Conditions (t=0 and t=1) ---
        // Most standard easing functions should return 0 at t=0 and 1 at t=1

        [Theory]
        [InlineData(0f)]
        [InlineData(1f)]
        public void EasingFunctions_ReturnCorrectBoundaryValues(float t)
        {
            float expected = t; // Expect 0 for t=0, 1 for t=1

            AssertFloatEqual(expected, Easing.Linear(t));
            AssertFloatEqual(expected, Easing.EaseInSine(t));
            AssertFloatEqual(expected, Easing.EaseOutSine(t));
            AssertFloatEqual(expected, Easing.EaseInOutSine(t));
            AssertFloatEqual(expected, Easing.EaseInQuad(t));
            AssertFloatEqual(expected, Easing.EaseOutQuad(t));
            AssertFloatEqual(expected, Easing.EaseInOutQuad(t));
            AssertFloatEqual(expected, Easing.EaseInCubic(t));
            AssertFloatEqual(expected, Easing.EaseOutCubic(t));
            AssertFloatEqual(expected, Easing.EaseInOutCubic(t));
            AssertFloatEqual(expected, Easing.EaseInExpo(t));
            AssertFloatEqual(expected, Easing.EaseOutExpo(t));
            AssertFloatEqual(expected, Easing.EaseInOutExpo(t));
            AssertFloatEqual(expected, Easing.EaseInBack(t));
            AssertFloatEqual(expected, Easing.EaseOutBack(t));
            AssertFloatEqual(expected, Easing.EaseInOutBack(t));
             AssertFloatEqual(expected, Easing.EaseInBounce(t));
             AssertFloatEqual(expected, Easing.EaseOutBounce(t));
             AssertFloatEqual(expected, Easing.EaseInOutBounce(t));
        }

        // --- Test Midpoint Values (t=0.5) ---
        // Helps verify the curve shape is roughly correct

        [Fact]
        public void EasingFunctions_MidpointValues()
        {
            float t = 0.5f;

            // Linear should be exactly 0.5
            AssertFloatEqual(0.5f, Easing.Linear(t));

            // EaseInOut functions should generally be 0.5 at t=0.5
            AssertFloatEqual(0.5f, Easing.EaseInOutSine(t));
            AssertFloatEqual(0.5f, Easing.EaseInOutQuad(t));
            AssertFloatEqual(0.5f, Easing.EaseInOutCubic(t));
            AssertFloatEqual(0.5f, Easing.EaseInOutExpo(t));
            AssertFloatEqual(0.5f, Easing.EaseInOutBack(t));
             AssertFloatEqual(0.5f, Easing.EaseInOutBounce(t));

            // EaseIn should be < 0.5 (starts slow)
            Assert.True(Easing.EaseInSine(t) < 0.5f);
            Assert.True(Easing.EaseInQuad(t) < 0.5f); // 0.25
            Assert.True(Easing.EaseInCubic(t) < 0.5f); // 0.125
            Assert.True(Easing.EaseInExpo(t) < 0.5f); // ~0.03
            Assert.True(Easing.EaseInBack(t) < 0.5f); // Can dip below 0 initially
            Assert.True(Easing.EaseInBounce(t) < 0.5f); // Stays low initially

            // EaseOut should be > 0.5 (starts fast)
            Assert.True(Easing.EaseOutSine(t) > 0.5f);
            Assert.True(Easing.EaseOutQuad(t) > 0.5f); // 0.75
            Assert.True(Easing.EaseOutCubic(t) > 0.5f); // 0.875
            Assert.True(Easing.EaseOutExpo(t) > 0.5f); // ~0.96
            Assert.True(Easing.EaseOutBack(t) > 0.5f); // Can overshoot 1
            Assert.True(Easing.EaseOutBounce(t) > 0.5f); // ~0.75 at first bounce point
        }

        // --- Specific Curve Checks (Optional) ---
        // Can add more specific checks if needed, e.g., verify overshoot for EaseOutBack

        [Fact]
        public void EaseOutBack_Overshoots()
        {
            // EaseOutBack should exceed 1 slightly during its curve
            bool didOvershoot = false;
            for (float t = 0.01f; t < 1.0f; t += 0.01f)
            {
                if (Easing.EaseOutBack(t) > 1.0f)
                {
                    didOvershoot = true;
                    break;
                }
            }
            Assert.True(didOvershoot, "EaseOutBack should overshoot 1.0");
        }

        [Fact]
        public void EaseInBack_Undershoots()
        {
            // EaseInBack should dip below 0 slightly during its curve
            bool didUndershoot = false;
            for (float t = 0.01f; t < 1.0f; t += 0.01f)
            {
                if (Easing.EaseInBack(t) < 0.0f)
                {
                    didUndershoot = true;
                    break;
                }
            }
            Assert.True(didUndershoot, "EaseInBack should undershoot 0.0");
        }
    }
}