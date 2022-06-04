using UnityEngine;

namespace STerrain.EndlessTerrain
{
    /// <summary>
    /// Parameters used to construct a terrain chunk.
    /// </summary>
    public class TerrainChunkParams
    {
        /// <summary>
        /// Parent transform for the terrain chunk game object.
        /// </summary>
        public Transform Parent { get; set; }

        /// <summary>
        /// Material of the terrain chunk.
        /// </summary>
        public Material Material { get; set; }

        /// <summary>
        /// Transform position of the terrain chunk.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Transform scale of the terrain chunk.
        /// </summary>
        public int Scale { get; set; }
    }
}