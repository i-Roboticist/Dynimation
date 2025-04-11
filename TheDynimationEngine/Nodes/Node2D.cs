// File: Nodes/Node2D.cs
using System;
using System.Numerics;
using TheDynimationEngine.Core; // Use the Node base class
using SkiaSharp; // Use SKMatrix for transformations

namespace TheDynimationEngine.Nodes
{
    public class Node2D : Node
    {
        // --- Local Transform Properties ---
        private Vector2 _position = Vector2.Zero;
        public Vector2 Position
        {
            get => _position;
            set { if (_position != value) { _position = value; /* MarkDirty(); */ } }
        }

        private float _rotationDegrees = 0f;
        public float RotationDegrees
        {
            get => _rotationDegrees;
            set { if (_rotationDegrees != value) { _rotationDegrees = value; /* MarkDirty(); */ } }
        }
        public float RotationRadians => MathF.PI / 180f * _rotationDegrees;

        private Vector2 _scale = Vector2.One;
        public Vector2 Scale
        {
            get => _scale;
            set { if (_scale != value) { _scale = value; /* MarkDirty(); */ } }
        }

        // --- Global Transform Properties (Read-only, calculated) ---
        public Vector2 GlobalPosition
        {
            get
            {
                var matrix = GetGlobalTransformMatrix();
                return new Vector2(matrix.TransX, matrix.TransY);
            }
        }
        public float GlobalRotationDegrees => GetGlobalRotationDegrees();
        public Vector2 GlobalScale => GetGlobalScale();

        // --- Transform Calculation ---
        // TODO: Implement caching based on dirty flags

        public SKMatrix GetLocalTransformMatrix()
        {
            // Implementation uses Concat(A, B) = B * A and calculates S * R * T via:
            // 1. matrix = T
            // 2. matrix = Concat(matrix, R) -> R * T
            // 3. matrix = Concat(matrix, S) -> S * (R * T) = S * R * T
            SKMatrix matrix = SKMatrix.CreateTranslation(Position.X, Position.Y); // T
            matrix = SKMatrix.Concat(matrix, SKMatrix.CreateRotationDegrees(RotationDegrees)); // R * T
            matrix = SKMatrix.Concat(matrix, SKMatrix.CreateScale(Scale.X, Scale.Y)); // S * (R * T)
            return matrix;
        }

        public SKMatrix GetGlobalTransformMatrix()
        {
            // TODO: Implement caching
            SKMatrix localMatrix = GetLocalTransformMatrix(); // S * R * T

            if (Parent is Node2D parent2D)
            {
                // --- CORRECTED GLOBAL TRANSFORM CALCULATION ---
                // We need ParentGlobal * Local = ParentGlobal * (S * R * T)
                // Use PostConcat: A.PostConcat(B) calculates A * B.
                SKMatrix parentGlobalMatrix = parent2D.GetGlobalTransformMatrix(); // Get parent's transform
                parentGlobalMatrix.PostConcat(localMatrix); // Calculates ParentGlobal * Local (modifies parentGlobalMatrix)
                return parentGlobalMatrix; // Return the result
            }
            else
            {
                return localMatrix; // No Node2D parent, global is local
            }
        }

        // --- Helper methods for extracting Global Rotation/Scale ---
        private float GetGlobalRotationDegrees()
        {
             var globalMatrix = GetGlobalTransformMatrix();
             // Note: This basic extraction might be inaccurate with shear
             return MathF.Atan2(globalMatrix.SkewY, globalMatrix.ScaleX) * (180f / MathF.PI);
        }
         private Vector2 GetGlobalScale()
         {
             var globalMatrix = GetGlobalTransformMatrix();
             // Note: This basic extraction might be inaccurate with shear
             float scaleX = MathF.Sqrt(globalMatrix.ScaleX * globalMatrix.ScaleX + globalMatrix.SkewY * globalMatrix.SkewY);
             float scaleY = MathF.Sqrt(globalMatrix.ScaleY * globalMatrix.ScaleY + globalMatrix.SkewX * globalMatrix.SkewX);
             return new Vector2(scaleX, scaleY);
         }

        // --- Transformation Methods ---
        public void Translate(Vector2 offset) { Position += offset; }
        public void Rotate(float degrees) { RotationDegrees += degrees; }

        // --- Drawing Integration ---
        public override void _Draw(SKCanvas canvas)
        {
            int saveCount = canvas.Save();
            SKMatrix globalMatrix = GetGlobalTransformMatrix();
            // --- Use 'in' modifier as suggested by warning ---
            canvas.Concat(in globalMatrix); // Use 'in' instead of 'ref'

            DrawSelf(canvas); // Call specific drawing logic

            var childrenCopy = Children.ToList();
            foreach (var child in childrenCopy)
            {
                if (child.SceneTree == this.SceneTree)
                {
                   child._Draw(canvas);
                }
            }
            canvas.RestoreToCount(saveCount);
        }

        public virtual void DrawSelf(SKCanvas canvas)
        {
            // Base implementation does nothing. Override in derived classes.
        }
    }
}