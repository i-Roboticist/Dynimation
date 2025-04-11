// File: TheDynimationEngine.Tests/Node2DTests.cs
using Xunit;
using TheDynimationEngine.Core;
using TheDynimationEngine.Nodes; // Use Node2D
using System.Numerics;
using SkiaSharp;

namespace TheDynimationEngine.Tests
{
    public class Node2DTests
    {
        private bool MatricesAreEqual(SKMatrix m1, SKMatrix m2, float tolerance = 1e-5f) // Slightly increased tolerance
        {
            return Math.Abs(m1.ScaleX - m2.ScaleX) < tolerance &&
                   Math.Abs(m1.SkewY - m2.SkewY) < tolerance &&
                   Math.Abs(m1.SkewX - m2.SkewX) < tolerance &&
                   Math.Abs(m1.ScaleY - m2.ScaleY) < tolerance &&
                   Math.Abs(m1.TransX - m2.TransX) < tolerance &&
                   Math.Abs(m1.TransY - m2.TransY) < tolerance &&
                   Math.Abs(m1.Persp0 - m2.Persp0) < tolerance &&
                   Math.Abs(m1.Persp1 - m2.Persp1) < tolerance &&
                   Math.Abs(m1.Persp2 - m2.Persp2) < tolerance;
        }

        [Fact]
        public void Node2D_Creation_Defaults()
        {
            var node2d = new Node2D();
            Assert.Equal(Vector2.Zero, node2d.Position);
            Assert.Equal(0f, node2d.RotationDegrees);
            Assert.Equal(Vector2.One, node2d.Scale);
            Assert.Equal(Vector2.Zero, node2d.GlobalPosition);
            Assert.Equal(0f, node2d.GlobalRotationDegrees);
            Assert.Equal(Vector2.One, node2d.GlobalScale);
        }

        [Fact]
        public void Node2D_SetProperties()
        {
            var node2d = new Node2D();
            var newPos = new Vector2(10, 20);
            var newRot = 45f;
            var newScale = new Vector2(2, 0.5f);
            node2d.Position = newPos;
            node2d.RotationDegrees = newRot;
            node2d.Scale = newScale;
            Assert.Equal(newPos, node2d.Position);
            Assert.Equal(newRot, node2d.RotationDegrees);
            Assert.Equal(newScale, node2d.Scale);
        }

        [Fact]
        public void Node2D_GetLocalTransformMatrix_Identity()
        {
            var node2d = new Node2D();
            var expectedMatrix = SKMatrix.Identity;
            var actualMatrix = node2d.GetLocalTransformMatrix();
            Assert.True(MatricesAreEqual(expectedMatrix, actualMatrix));
        }

        [Fact]
        public void Node2D_GetLocalTransformMatrix_Translation()
        {
            var node2d = new Node2D { Position = new Vector2(50, -30) };
            var expectedMatrix = SKMatrix.CreateTranslation(50, -30);
            var actualMatrix = node2d.GetLocalTransformMatrix();
            Assert.True(MatricesAreEqual(expectedMatrix, actualMatrix));
        }

        [Fact]
        public void Node2D_GetLocalTransformMatrix_Rotation()
        {
            var node2d = new Node2D { RotationDegrees = 90f };
            var expectedMatrix = SKMatrix.CreateRotationDegrees(90f);
            var actualMatrix = node2d.GetLocalTransformMatrix();
            Assert.True(MatricesAreEqual(expectedMatrix, actualMatrix), $"Expected:\n{expectedMatrix}\nActual:\n{actualMatrix}");
        }

        [Fact]
        public void Node2D_GetLocalTransformMatrix_Scale()
        {
            var node2d = new Node2D { Scale = new Vector2(3, 2) };
            var expectedMatrix = SKMatrix.CreateScale(3, 2);
            var actualMatrix = node2d.GetLocalTransformMatrix();
            Assert.True(MatricesAreEqual(expectedMatrix, actualMatrix));
        }

        [Fact]
        public void Node2D_GetLocalTransformMatrix_Combined()
        {
            // Arrange
            var node2d = new Node2D
            {
                Position = new Vector2(10, 20),
                RotationDegrees = 45f,
                Scale = new Vector2(2, 1)
            };

            // Act
            var actualMatrix = node2d.GetLocalTransformMatrix();

            // --- Calculate Expected matrix using the *exact same Concat steps* as implementation ---
            // Implementation calculates S * R * T via sequence of Concats where Concat(A,B)=B*A
            var expectedMatrix = SKMatrix.CreateTranslation(10, 20); // Step 1: T
            expectedMatrix = SKMatrix.Concat(expectedMatrix, SKMatrix.CreateRotationDegrees(45f)); // Step 2: R * T
            expectedMatrix = SKMatrix.Concat(expectedMatrix, SKMatrix.CreateScale(2, 1)); // Step 3: S * (R * T)

            // Assert
            Assert.True(MatricesAreEqual(expectedMatrix, actualMatrix), $"Expected (S*R*T):\n{expectedMatrix}\nActual:\n{actualMatrix}");
        }

        [Fact]
        public void Node2D_GetGlobalTransformMatrix_NoParent()
        {
            var node2d = new Node2D { Position = new Vector2(5, 5) };
            var expectedMatrix = node2d.GetLocalTransformMatrix();
            var actualMatrix = node2d.GetGlobalTransformMatrix();
            Assert.True(MatricesAreEqual(expectedMatrix, actualMatrix));
        }

        [Fact]
        public void Node2D_GetGlobalTransformMatrix_WithParent()
        {
            // Arrange
            var parent = new Node2D { Position = new Vector2(100, 50), RotationDegrees = 90f };
            var child = new Node2D { Position = new Vector2(10, 0), Scale = new Vector2(2, 1) };
            parent.AddChild(child);

            // Act
            var actualGlobalMatrix = child.GetGlobalTransformMatrix(); // Get from implementation (now fixed)

            // Expected Global Matrix = Parent Global * Child Local
            var parentGlobalMatrix = parent.GetGlobalTransformMatrix(); // Parent's world matrix
            var childLocalMatrix = child.GetLocalTransformMatrix(); // Child's local SRT matrix
            // Calculate ParentGlobal * ChildLocal using PostConcat (A.PostConcat(B) = A * B)
            var expectedGlobalMatrix = parentGlobalMatrix; // Start with parent's global
            expectedGlobalMatrix.PostConcat(childLocalMatrix); // Apply child's local transform

            // --- Assert Matrix ---
            Assert.True(MatricesAreEqual(expectedGlobalMatrix, actualGlobalMatrix), $"Expected Global Matrix:\n{expectedGlobalMatrix}\nActual Global Matrix:\n{actualGlobalMatrix}");

            // --- Assert Position derived from the calculated expected global matrix ---
            var expectedPosition = new Vector2(expectedGlobalMatrix.TransX, expectedGlobalMatrix.TransY);
            var actualPosition = child.GlobalPosition;
            float tolerance = 1e-5f;
            Assert.True(Math.Abs(expectedPosition.X - actualPosition.X) < tolerance, $"X Position Mismatch. Expected: {expectedPosition.X}, Actual: {actualPosition.X}");
            Assert.True(Math.Abs(expectedPosition.Y - actualPosition.Y) < tolerance, $"Y Position Mismatch. Expected: {expectedPosition.Y}, Actual: {actualPosition.Y}");
        }

        [Fact]
        public void Node2D_Translate()
        {
            var node2d = new Node2D { Position = new Vector2(10, 10) };
            var offset = new Vector2(5, -2);
            var expectedPos = new Vector2(15, 8);
            node2d.Translate(offset);
            Assert.Equal(expectedPos, node2d.Position);
        }

        [Fact]
        public void Node2D_Rotate()
        {
            var node2d = new Node2D { RotationDegrees = 30f };
            var rotationAmount = 60f;
            var expectedRot = 90f;
            node2d.Rotate(rotationAmount);
            Assert.Equal(expectedRot, node2d.RotationDegrees);
        }
    }
}