// File: Core/SceneTree.cs
using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp; // For DrawFrame signature

namespace TheDynimationEngine.Core
{
    /// <summary>
    /// Manages the currently active tree of Nodes.
    /// Handles node processing, lifecycle events (calling _EnterTree, _Ready, _Process, etc.),
    /// group management, and safe node removal (QueueFree).
    /// Typically, only one SceneTree is active at a time for rendering/processing.
    /// </summary>
    public class SceneTree
    {
        /// <summary>
        /// Gets the root node of the active scene tree.
        /// </summary>
        public Node Root { get; private set; }

        // Internal collections for managing groups and queued deletions
        private readonly Dictionary<string, HashSet<Node>> _groups = new Dictionary<string, HashSet<Node>>();
        private readonly List<Node> _nodesToFree = new List<Node>();
        private bool _isProcessingFrame = false; // Flag to prevent issues during QueueFree

        // Potential Singletons/Servers (placeholder for now)
        // public RenderingServer RenderingServer { get; } = new RenderingServer();
        // public PhysicsServer2D PhysicsServer { get; } = new PhysicsServer2D();
        // public AssetLoader AssetLoader { get; } = new AssetLoader();
        // public TweenManager TweenManager { get; } = new TweenManager();

        /// <summary>
        /// Creates a new SceneTree with the specified root node.
        /// Automatically triggers the _EnterTree and _Ready lifecycle calls for the entire tree.
        /// </summary>
        /// <param name="rootNode">The root node of the scene.</param>
        /// <exception cref="ArgumentNullException">Thrown if rootNode is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if rootNode is already part of another SceneTree.</exception>
        public SceneTree(Node rootNode)
        {
            if (rootNode == null) throw new ArgumentNullException(nameof(rootNode));
            if (rootNode.SceneTree != null) throw new InvalidOperationException("Root node provided is already part of a SceneTree.");

            Root = rootNode;
            PropagateEnterTree(Root); // Start the enter/ready cascade
            // Note: Ready is called *after* EnterTree finishes for the whole branch
            CallReady(Root);
        }

        // --- Lifecycle Propagation ---

        /// <summary>
        /// Internal method to recursively set the SceneTree reference and call _EnterTree.
        /// </summary>
        internal void PropagateEnterTree(Node node)
        {
            if (node.SceneTree != null) return; // Already in this or another tree (shouldn't happen with AddChild checks)

            node.SceneTree = this;
            node._EnterTree();

            // Add node to its predefined groups
            foreach (var groupName in node.Groups)
            {
                RegisterNodeInGroup(groupName, node);
            }

            // Recurse to children
            foreach (var child in node.Children)
            {
                PropagateEnterTree(child);
            }
        }

         /// <summary>
        /// Internal method to recursively call _Ready on nodes after _EnterTree is complete for the branch.
        /// </summary>
         private void CallReady(Node node)
         {
             // Call _Ready on children first (depth-first)
             foreach (var child in node.Children)
             {
                 CallReady(child);
             }
             // Then call _Ready on the node itself
             node._Ready();
         }


        /// <summary>
        /// Internal method to recursively clear the SceneTree reference and call _ExitTree.
        /// </summary>
        internal void PropagateExitTree(Node node)
        {
            if (node.SceneTree != this) return; // Not part of this tree

            // Recurse to children first (clean up bottom-up)
            foreach (var child in node.Children)
            {
                PropagateExitTree(child);
            }

            // Call exit tree on the node itself
            node._ExitTree();

            // Remove node from all groups it belonged to
            // Make a copy of groups as _ExitTree might modify the collection
            var groupsToRemoveFrom = node.Groups.ToList();
            foreach (var groupName in groupsToRemoveFrom)
            {
                UnregisterNodeFromGroup(groupName, node);
            }

            node.SceneTree = null; // Node is no longer in the tree
        }

        // --- Frame Processing ---

        /// <summary>
        /// Processes a single logic frame for the entire scene tree.
        /// Calls _Process on all nodes and handles queued deletions.
        /// </summary>
        /// <param name="delta">Time elapsed since the last logic frame.</param>
        public void ProcessFrame(float delta)
        {
            _isProcessingFrame = true;
            try
            {
                // Process nodes recursively
                ProcessNode(Root, delta);

                // TODO: Physics tick might happen here based on accumulated time
                 // float physicsDelta = GetPhysicsTimeStep(); // Example
                 // PhysicsProcessNode(Root, physicsDelta);

                 // TODO: Update tweens via TweenManager?
            }
            finally
            {
                _isProcessingFrame = false;
                FreeQueuedNodes(); // Handle nodes marked for deletion
            }
        }

        private void ProcessNode(Node node, float delta)
        {
            // Process self first
            node._Process(delta);
            // Then process children
            // Create copy in case _Process modifies child list (e.g., via QueueFree)
            var childrenCopy = node.Children.ToList();
            foreach (var child in childrenCopy)
            {
                 // Check if child was freed during parent's process or sibling's process
                 if (child.SceneTree == this)
                 {
                    ProcessNode(child, delta);
                 }
            }
        }

        private void PhysicsProcessNode(Node node, float physicsDelta)
        {
             // Process self first
            node._PhysicsProcess(physicsDelta);
            // Then process children
             var childrenCopy = node.Children.ToList();
            foreach (var child in childrenCopy)
            {
                 if (child.SceneTree == this)
                 {
                    PhysicsProcessNode(child, physicsDelta);
                 }
            }
        }


        /// <summary>
        /// Processes a single drawing frame for the entire scene tree.
        /// Calls _Draw on all nodes.
        /// </summary>
        /// <param name="canvas">The SkiaSharp canvas to draw on.</param>
        public void DrawFrame(SKCanvas canvas)
        {
            if (canvas == null) return;

            // Reset canvas state if needed, or handled by RenderingServer
            // RenderingServer.BeginFrame(canvas);

            // Draw nodes recursively
            DrawNode(Root, canvas);

            // RenderingServer.EndFrame();
        }

        private void DrawNode(Node node, SKCanvas canvas)
        {
            // TODO: Apply node's transform (from Node2D) before drawing self and children
            // canvas.Save();
            // ApplyTransform(canvas, node); // Hypothetical function

            node._Draw(canvas); // Let the node draw itself

            // Draw children recursively
            // Must use read-only access or copy if _Draw could modify children
             var childrenCopy = node.Children.ToList(); // Safer if _Draw can modify tree
            foreach (var child in childrenCopy)
            {
                if(child.SceneTree == this) // Check if still in tree
                {
                   DrawNode(child, canvas);
                }
            }

            // canvas.Restore(); // Restore transform state
        }


        // --- Group Management ---

        internal void RegisterNodeInGroup(string groupName, Node node)
        {
            if (!_groups.TryGetValue(groupName, out var nodeList))
            {
                nodeList = new HashSet<Node>();
                _groups[groupName] = nodeList;
            }
            nodeList.Add(node);
        }

        internal void UnregisterNodeFromGroup(string groupName, Node node)
        {
            if (_groups.TryGetValue(groupName, out var nodeList))
            {
                nodeList.Remove(node);
                // Optional: Remove group entry if list becomes empty
                if (nodeList.Count == 0)
                {
                    _groups.Remove(groupName);
                }
            }
        }

        /// <summary>
        /// Gets an enumeration of all nodes currently in the specified group.
        /// Returns an empty enumeration if the group doesn't exist.
        /// </summary>
        /// <param name="groupName">The name of the group.</param>
        /// <returns>An enumerable collection of nodes in the group.</returns>
        public IEnumerable<Node> GetNodesInGroup(string groupName)
        {
            if (_groups.TryGetValue(groupName, out var nodeList))
            {
                // Return a defensive copy or ensure the caller doesn't modify the HashSet
                return nodeList.ToList(); // ToList creates a copy
            }
            return Enumerable.Empty<Node>();
        }

        // --- Safe Node Removal ---

        /// <summary>
        /// Adds a node to the queue for deletion at the end of the current frame.
        /// Called by Node.QueueFree().
        /// </summary>
        /// <param name="node">The node to queue for deletion.</param>
        internal void QueueFree(Node node)
        {
            if (node == null || node.SceneTree != this) return; // Not in this tree

            // If processing, add to queue. Otherwise (e.g., called outside loop), free immediately?
            // For simplicity, always queue if called via Node.QueueFree()
            if (!_nodesToFree.Contains(node)) // Avoid duplicates
            {
                _nodesToFree.Add(node);
            }
        }

        /// <summary>
        /// Processes the queue of nodes marked for deletion.
        /// Called automatically at the end of ProcessFrame.
        /// </summary>
        private void FreeQueuedNodes()
        {
            if (_nodesToFree.Count == 0) return;

            // Use a separate list to iterate while modifying the main queue potentially
            var nodesToProcess = _nodesToFree.ToList();
            _nodesToFree.Clear(); // Clear original queue

            foreach (var node in nodesToProcess)
            {
                // Ensure the node wasn't already removed by an earlier free operation
                if (node.Parent != null && node.SceneTree == this)
                {
                    node.Parent.RemoveChild(node); // This triggers PropagateExitTree
                }
                // If it's the root node being freed, handle appropriately (maybe signal application?)
                 else if (node == Root && node.SceneTree == this)
                 {
                     PropagateExitTree(node); // Ensure cleanup
                     // Signal or handle root node deletion - maybe throw exception or set a flag?
                     Console.WriteLine("Warning: Root node was freed.");
                     // Root = null; // Or replace with a placeholder? Needs defined behavior.
                 }
            }
             // If _nodesToFree was added to during the loop (unlikely but possible), process again? No, let next frame handle.
        }

        // --- Scene Query API (Placeholders - Requires Spatial Indexing) ---
        // public IEnumerable<T> FindNodesInRadius<T>(Vector2 center, float radius) where T : Node { /* ... */ }
        // public IEnumerable<T> FindNodesInGroupRadius<T>(string group, Vector2 center, float radius) where T : Node { /* ... */ }
    }
}