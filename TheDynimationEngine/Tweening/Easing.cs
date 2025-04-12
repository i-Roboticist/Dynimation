// File: Tweening/Easing.cs
using System;
using System.Numerics; // Although not directly used here, easing often applies to vectors

namespace TheDynimationEngine.Tweening
{
    /// <summary>
    /// Provides standard easing functions for tweening.
    /// Easing functions take a progress value 't' (typically 0 to 1) and return
    /// an eased value (usually also 0 to 1, but can overshoot for elastic/bounce).
    /// </summary>
    public static class Easing
    {
        // Based on equations from https://easings.net/

        // --- Linear ---
        public static float Linear(float t) => t;

        // --- Sine ---
        public static float EaseInSine(float t) => 1f - MathF.Cos((t * MathF.PI) / 2f);
        public static float EaseOutSine(float t) => MathF.Sin((t * MathF.PI) / 2f);
        public static float EaseInOutSine(float t) => -(MathF.Cos(MathF.PI * t) - 1f) / 2f;

        // --- Quad (Quadratic) ---
        public static float EaseInQuad(float t) => t * t;
        public static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
        public static float EaseInOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - MathF.Pow(-2f * t + 2f, 2) / 2f;

        // --- Cubic ---
        public static float EaseInCubic(float t) => t * t * t;
        public static float EaseOutCubic(float t) => 1f - MathF.Pow(1f - t, 3);
        public static float EaseInOutCubic(float t) => t < 0.5f ? 4f * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 3) / 2f;

        // --- Expo ---
        // Removed the erroneous first definition with '==='
        // Keep the second, corrected definition:
        public static float EaseInExpo(float t) => t <= 0f ? 0f : MathF.Pow(2f, 10f * (t - 1f));
        public static float EaseOutExpo(float t) => t >= 1f ? 1f : 1f - MathF.Pow(2f, -10f * t);
        public static float EaseInOutExpo(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return t < 0.5f ? MathF.Pow(2f, 20f * t - 10f) / 2f
                           : (2f - MathF.Pow(2f, -20f * t + 10f)) / 2f;
        }

        // --- Back ---
        private const float c1 = 1.70158f;
        private const float c3 = c1 + 1f;

        public static float EaseInBack(float t) => c3 * t * t * t - c1 * t * t;
        public static float EaseOutBack(float t) => 1f + c3 * MathF.Pow(t - 1f, 3) + c1 * MathF.Pow(t - 1f, 2);
        public static float EaseInOutBack(float t)
        {
            const float c2 = c1 * 1.525f;
            return t < 0.5f
              ? (MathF.Pow(2f * t, 2) * ((c2 + 1f) * 2f * t - c2)) / 2f
              : (MathF.Pow(2f * t - 2f, 2) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) / 2f;
        }

        // --- Bounce ---
        private const float n1 = 7.5625f;
        private const float d1 = 2.75f;

        public static float EaseOutBounce(float t)
        {
            if (t < 1f / d1) {
                return n1 * t * t;
            } else if (t < 2f / d1) {
                // Use temporary variable to avoid modifying 't' across checks if C# evaluates -= inline
                float t_adj = t - (1.5f / d1);
                return n1 * t_adj * t_adj + 0.75f;
            } else if (t < 2.5f / d1) {
                 float t_adj = t - (2.25f / d1);
                return n1 * t_adj * t_adj + 0.9375f;
            } else {
                 float t_adj = t - (2.625f / d1);
                return n1 * t_adj * t_adj + 0.984375f;
            }
        }
         public static float EaseInBounce(float t) => 1f - EaseOutBounce(1f - t);
         public static float EaseInOutBounce(float t) => t < 0.5f
            ? (1f - EaseOutBounce(1f - 2f * t)) / 2f
            : (1f + EaseOutBounce(2f * t - 1f)) / 2f;

        // Add more functions as needed (Elastic, Circ, etc.)
    }
}