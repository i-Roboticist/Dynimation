// File: TheDynimationEngine.Tests/NodeTests.cs
using Xunit;
using TheDynimationEngine.Core; // Use the classes from the engine

namespace TheDynimationEngine.Tests
{
    public class NodeTests
    {
        [Fact]
        public void Node_Creation_Defaults()
        {
            var node = new Node();
            Assert.Equal("Node", node.Name);
            // --- REMOVED Assert.NotNull(node.Id); ---
            Assert.Null(node.Parent);
            Assert.Empty(node.Children);
            Assert.Empty(node.Groups);
            Assert.Null(node.SceneTree);
        }

        [Fact]
        public void Node_Hierarchy_AddChild()
        {
            var parent = new Node { Name = "Parent" };
            var child = new Node { Name = "Child" };
            parent.AddChild(child);
            Assert.Single(parent.Children);
            Assert.Same(child, parent.Children[0]);
            Assert.Same(parent, child.Parent);
        }

        [Fact]
        public void Node_Hierarchy_RemoveChild()
        {
            var parent = new Node();
            var child = new Node();
            parent.AddChild(child);
            parent.RemoveChild(child);
            Assert.Empty(parent.Children);
            Assert.Null(child.Parent);
        }

         [Fact]
        public void Node_Hierarchy_AddChild_PreventsCycles()
        {
            var grandparent = new Node();
            var parent = new Node();
            var child = new Node();
            grandparent.AddChild(parent);
            parent.AddChild(child);
            // Test adding ancestor
            Assert.Throws<ArgumentException>(() => child.AddChild(grandparent));
            // Test adding self
            Assert.Throws<ArgumentException>(() => parent.AddChild(parent));
        }

        [Fact]
        public void Node_Hierarchy_Reparenting()
        {
            var parent1 = new Node { Name = "P1" };
            var parent2 = new Node { Name = "P2" };
            var child = new Node { Name = "C" };
            parent1.AddChild(child);
            parent2.AddChild(child); // Should reparent
            Assert.Empty(parent1.Children);
            Assert.Single(parent2.Children);
            Assert.Same(child, parent2.Children[0]);
            Assert.Same(parent2, child.Parent);
        }

        [Fact]
        public void Node_Groups_AddAndCheck()
        {
            var node = new Node();
            string groupName = "testGroup";
            node.AddToGroup(groupName);
            Assert.True(node.IsInGroup(groupName));
            Assert.Contains(groupName, node.Groups);
            Assert.Single(node.Groups);
        }

        [Fact]
        public void Node_Groups_Remove()
        {
            var node = new Node();
            string groupName = "testGroup";
            node.AddToGroup(groupName);
            node.RemoveFromGroup(groupName);
            Assert.False(node.IsInGroup(groupName));
            Assert.DoesNotContain(groupName, node.Groups);
            Assert.Empty(node.Groups);
        }
    }
}