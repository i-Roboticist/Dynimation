// File: IO/AssetLoader.cs
using System;
using System.IO;
using TheDynimationEngine.Rendering; // Needs Texture

namespace TheDynimationEngine.IO
{
    /// <summary>
    /// Handles loading engine resources (like Textures) from the filesystem.
    /// NOTE: This is a very basic implementation. A real AssetLoader would
    /// likely involve caching, asynchronous loading, and potentially support
    /// for asset bundles or more complex path resolution (e.g., "res://").
    /// </summary>
    public static class AssetLoader
    {
        // Potential future: Set a base path for assets
        // public static string BaseAssetPath { get; set; } = Directory.GetCurrentDirectory();

        /// <summary>
        /// Loads a resource of the specified type from the given path.
        /// Currently only supports loading Textures.
        /// Paths are assumed to be relative to the current working directory or absolute.
        /// </summary>
        /// <typeparam name="T">The type of resource to load (currently only Texture).</typeparam>
        /// <param name="path">The file path to the resource.</param>
        /// <returns>The loaded resource, or null if loading fails or the type is unsupported.</returns>
        public static T? Load<T>(string path) where T : class, IDisposable
        {
            // TODO: Implement caching based on path to avoid reloading the same asset.
            // Dictionary<string, WeakReference<IDisposable>> _cache = ...;

            if (typeof(T) == typeof(Texture))
            {
                // Attempt to load as Texture using its static method
                Texture? texture = Texture.LoadFromFile(path);
                // We need to cast to T?. 'as T' works for reference types.
                return texture as T;
            }
            // Add cases for other resource types here later (e.g., Fonts, Materials)
            // else if (typeof(T) == typeof(Font)) { ... }

            else
            {
                Console.WriteLine($"Error: AssetLoader does not support loading type '{typeof(T).Name}' yet.");
                return null;
            }
        }

        // Potential future method to resolve paths like "res://Sprites/player.png"
        // private static string ResolvePath(string path) { ... }
    }
}