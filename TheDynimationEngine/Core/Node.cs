// File: Core/Node.cs
using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp; // Required for SKCanvas in the _Draw method signature

namespace TheDynimationEngine.Core
{
    /// <summary>
    /// The base class for all objects in the Scene Tree.
    /// Nodes provide core functionality like hierarchy management (parent/children),
    /// lifecycle callbacks, group membership, and access to the SceneTree.
    /// </summary>
    public class Node
    {
        // --- Properties ---
        public string Name { get; set; } = "Node";
        public Guid Id { get; } = Guid.NewGuid();
        public SceneTree? SceneTree { get; internal set; } = null;
        public Node? Parent { get; private set; } = null;
        private readonly List<Node> _children = new List<Node>();
        private readonly HashSet<string> _groups = new HashSet<string>();
        public IReadOnlyList<Node> Children => _children.AsReadOnly();
        public IReadOnlyCollection<string> Groups => _groups;

        // --- Hierarchy Management ---
        public void AddChild(Node child)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            if (child == this) throw new ArgumentException("Cannot add a node as a child of itself.", nameof(child));
            // --- CORRECTED CYCLE CHECK ---
            // Check if the 'child' node is already an ancestor of 'this' node
            if (child.IsAncestorOf(this)) throw new ArgumentException("Cannot add an ancestor node as a child (creates cycle).", nameof(child));

            child.Parent?.RemoveChildInternal(child, keepInTree: true);

            child.Parent = this;
            _children.Add(child);

            if (this.SceneTree != null && child.SceneTree == null)
            {
                this.SceneTree.PropagateEnterTree(child);
            }
        }

        public void RemoveChild(Node child)
        {
            RemoveChildInternal(child, keepInTree: false);
        }

        private void RemoveChildInternal(Node child, bool keepInTree)
        {
            if (child == null || child.Parent != this) return;

            child.Parent = null;
            _children.Remove(child);

            if (!keepInTree && this.SceneTree != null)
            {
                this.SceneTree.PropagateExitTree(child);
            }
        }

        public void RemoveAllChildren()
        {
             for(int i = _children.Count - 1; i >= 0; i--)
             {
                 RemoveChild(_children[i]);
             }
        }

        public bool IsAncestorOf(Node node)
        {
            Node? p = node.Parent;
            while (p != null)
            {
                if (p == this) return true;
                p = p.Parent;
            }
            return false;
        }

        public Node? GetChild(string name, bool recursive = false)
        {
             foreach(var child in _children)
             {
                 if (child.Name == name) return child;
                 if(recursive)
                 {
                     var found = child.GetChild(name, true);
                     if (found != null) return found;
                 }
             }
             return null;
        }

        public Node GetChild(int index)
        {
            if (index < 0 || index >= _children.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _children[index];
        }

        public int GetChildCount() => _children.Count;

        // --- Group Management ---
        public void AddToGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return;
            if (_groups.Add(groupName))
            {
                SceneTree?.RegisterNodeInGroup(groupName, this);
            }
        }

        public void RemoveFromGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return;
            if (_groups.Remove(groupName))
            {
                SceneTree?.UnregisterNodeFromGroup(groupName, this);
            }
        }

        public bool IsInGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return false;
            return _groups.Contains(groupName);
        }

        // --- Lifecycle Methods (Called by SceneTree) ---
        public virtual void _EnterTree() { }
        public virtual void _Ready() { }
        public virtual void _Process(float delta) { }
        public virtual void _PhysicsProcess(float delta) { }
        public virtual void _Draw(SKCanvas canvas) { }
        public virtual void _ExitTree() { }

        // --- Utility ---
        public void QueueFree()
        {
            SceneTree?.QueueFree(this);
        }
        public override string ToString()
        {
            return $"{Name} ({GetType().Name}) [{Id.ToString().Substring(0, 8)}]";
        }
    }
}