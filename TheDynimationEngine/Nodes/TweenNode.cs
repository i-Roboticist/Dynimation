// File: Nodes/TweenNode.cs
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using TheDynimationEngine.Core;
using TheDynimationEngine.Tweening;
using SkiaSharp; // For SKColor interpolation

namespace TheDynimationEngine.Nodes
{
    /// <summary>
    /// Delegate type for easing functions used by TweenNode.
    /// Matches the signature of methods in the Easing class.
    /// </summary>
    /// <param name="t">Normalized time (0 to 1).</param>
    /// <returns>Eased progress value.</returns>
    public delegate float EasingFunc(float t);

    /// <summary>
    /// A Node that handles interpolating properties of other nodes over time (tweening).
    /// Create this node, configure tweens using its methods (e.g., TweenProperty),
    /// add it to the SceneTree (often as a child of the node being animated), and call Start().
    /// </summary>
    public class TweenNode : Node
    {
        private enum TweenState
        {
            Idle,
            Running,
            Paused,
            Finished
        }

        // Represents a single property being tweened
        private class PropertyTween
        {
            public object Target { get; }
            public PropertyInfo Property { get; }
            public object StartValue { get; set; } = null!; // Set when tween starts
            public object EndValue { get; }
            public float Duration { get; }
            public float ElapsedTime { get; set; }
            public EasingFunc EaseFunc { get; }
            public float Delay { get; }

            public PropertyTween(object target, PropertyInfo property, object endValue, float duration, float delay, EasingFunc easeFunc)
            {
                Target = target;
                Property = property;
                EndValue = endValue;
                Duration = Math.Max(0.001f, duration); // Avoid zero duration
                Delay = Math.Max(0f, delay);
                EaseFunc = easeFunc;
                ElapsedTime = -Delay; // Start elapsed time negative to account for delay
            }

            // Updates the property based on delta time. Returns true if finished.
            public bool Update(float delta)
            {
                ElapsedTime += delta;

                // Handle delay
                if (ElapsedTime < 0f)
                {
                    return false; // Still in delay phase
                }

                // Capture start value on the first update after delay
                if (StartValue == null)
                {
                     try
                     {
                         StartValue = Property.GetValue(Target) ?? throw new InvalidOperationException("Property returned null.");
                     }
                     catch (Exception ex)
                     {
                         // Log error and stop this specific tween
                         Console.WriteLine($"Error getting start value for tween on {Target.GetType().Name}.{Property.Name}: {ex.Message}");
                         ElapsedTime = Duration; // Mark as finished to prevent further errors
                         return true;
                     }
                }

                // Calculate normalized time (clamped 0 to 1)
                float t = Math.Clamp(ElapsedTime / Duration, 0f, 1f);
                // Apply easing
                float easedT = EaseFunc(t);

                // Interpolate and set value based on type
                try
                {
                    object interpolatedValue = Interpolate(StartValue, EndValue, easedT);
                    Property.SetValue(Target, interpolatedValue);
                }
                 catch (Exception ex)
                 {
                     Console.WriteLine($"Error setting value for tween on {Target.GetType().Name}.{Property.Name}: {ex.Message}");
                     ElapsedTime = Duration; // Mark as finished
                     return true;
                 }


                return ElapsedTime >= Duration; // Finished?
            }

            // Simple interpolation logic - extend for more types
            private static object Interpolate(object start, object end, float t)
            {
                return start switch
                {
                    float startFloat when end is float endFloat => startFloat + (endFloat - startFloat) * t,
                    Vector2 startVec when end is Vector2 endVec => Vector2.Lerp(startVec, endVec, t),
                    SKColor startCol when end is SKColor endCol => InterpolateColor(startCol, endCol, t),
                    // Add Vector3, double, int (maybe cast to float?), etc.
                    _ => t < 0.5f ? start : end // Default: just snap at midpoint if type unknown
                };
            }
             private static SKColor InterpolateColor(SKColor start, SKColor end, float t)
             {
                 byte r = (byte)(start.Red + (end.Red - start.Red) * t);
                 byte g = (byte)(start.Green + (end.Green - start.Green) * t);
                 byte b = (byte)(start.Blue + (end.Blue - start.Blue) * t);
                 byte a = (byte)(start.Alpha + (end.Alpha - start.Alpha) * t);
                 return new SKColor(r, g, b, a);
             }
        }

        private readonly List<PropertyTween> _tweens = new List<PropertyTween>();
        private TweenState _state = TweenState.Idle;
        private bool _autoDeleteOnFinish = true; // Remove node when tween completes?

        // --- Configuration Methods (Fluent API style) ---

        /// <summary>
        /// Adds a property to be tweened.
        /// </summary>
        /// <param name="target">The object instance whose property will be animated.</param>
        /// <param name="propertyName">The name of the property (e.g., "Position", "RotationDegrees", "Color"). Must have public get/set.</param>
        /// <param name="endValue">The final value the property should reach.</param>
        /// <param name="duration">The time in seconds the tween should take.</param>
        /// <param name="delay">An optional delay in seconds before the tween starts.</param>
        /// <param name="easeFunc">The easing function to use (e.g., Easing.EaseOutQuad). Defaults to Easing.Linear.</param>
        /// <returns>This TweenNode instance for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown if the property is not found or is not writable.</exception>
        public TweenNode TweenProperty(object target, string propertyName, object endValue, float duration, float delay = 0f, EasingFunc? easeFunc = null)
        {
            if (_state != TweenState.Idle)
            {
                Console.WriteLine("Warning: Cannot add tweens while TweenNode is running or paused.");
                return this;
            }
            if (target == null) throw new ArgumentNullException(nameof(target));

            var propertyInfo = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo == null)
            {
                throw new ArgumentException($"Property '{propertyName}' not found on type '{target.GetType().Name}'.");
            }
            if (!propertyInfo.CanWrite)
            {
                throw new ArgumentException($"Property '{propertyName}' on type '{target.GetType().Name}' does not have a public setter.");
            }
            if (!propertyInfo.CanRead) // Need getter for StartValue
            {
                 throw new ArgumentException($"Property '{propertyName}' on type '{target.GetType().Name}' does not have a public getter.");
            }

            _tweens.Add(new PropertyTween(target, propertyInfo, endValue, duration, delay, easeFunc ?? Easing.Linear));
            return this;
        }

        /// <summary>
        /// Sets whether the TweenNode should automatically remove itself from the scene
        /// using QueueFree() when all its tweens are finished. Default is true.
        /// </summary>
        /// <param name="autoDelete">True to automatically delete, false otherwise.</param>
        /// <returns>This TweenNode instance for chaining.</returns>
        public TweenNode SetAutoDelete(bool autoDelete)
        {
             if (_state != TweenState.Idle) Console.WriteLine("Warning: SetAutoDelete should ideally be called before starting.");
             _autoDeleteOnFinish = autoDelete;
             return this;
        }

        // TODO: Add methods for TweenCallback, Sequence, Parallel if needed later


        // --- Control Methods ---

        /// <summary>
        /// Starts playing the configured tweens. Can only be called when Idle or Finished.
        /// </summary>
        public void Start()
        {
            if (_state == TweenState.Running || _state == TweenState.Paused)
            {
                Console.WriteLine("TweenNode already started or paused. Call Reset() first to restart.");
                return;
            }
            if (_tweens.Count == 0)
            {
                 Console.WriteLine("Warning: Starting TweenNode with no tweens configured.");
                 _state = TweenState.Finished;
                 if(_autoDeleteOnFinish) QueueFree();
                 return;
            }

            // Reset elapsed time for all tweens (accounts for delay)
             foreach (var pt in _tweens)
             {
                 pt.ElapsedTime = -pt.Delay;
                 pt.StartValue = null!; // Reset start value so it's grabbed on first update
             }

            _state = TweenState.Running;
        }

        /// <summary>
        /// Stops the tween immediately. Does not trigger finish events/callbacks.
        /// Resets the node to Idle state.
        /// </summary>
        public void Stop()
        {
             _state = TweenState.Idle;
              // Optionally reset properties to start values? Or leave them as they are? Leave them for now.
        }

         /// <summary>
        /// Resets the TweenNode to its initial state (Idle) allowing Start() to be called again.
        /// Resets progress of all configured tweens.
        /// </summary>
        public void Reset()
        {
             Stop(); // Use Stop to transition to Idle
              foreach (var pt in _tweens)
             {
                 pt.ElapsedTime = -pt.Delay; // Reset time back including delay
                 pt.StartValue = null!;
             }
        }


        // --- Node Lifecycle ---

        /// <summary>
        /// Called every frame by the SceneTree. Updates the active tweens.
        /// </summary>
        /// <param name="delta">Time since last frame.</param>
        public override void _Process(float delta)
        {
            if (_state != TweenState.Running) return; // Only process when running

            bool allFinished = true;
            // Iterate backwards allows removing finished tweens if needed, but we just check state
            for (int i = 0; i < _tweens.Count; i++)
            {
                if (_tweens[i].ElapsedTime < _tweens[i].Duration) // Check before updating
                {
                    bool finishedThisFrame = _tweens[i].Update(delta);
                     if (!finishedThisFrame) // If still not finished after update
                     {
                          allFinished = false;
                     }
                 }
                 // else: already finished, don't update, keep allFinished = true if it was
            }

            if (allFinished)
            {
                _state = TweenState.Finished;
                // TODO: Emit a "TweenFinished" signal/event?
                if (_autoDeleteOnFinish)
                {
                    QueueFree(); // Remove self from tree
                }
            }
        }
    }
}