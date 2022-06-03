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
        /// The transform scale of the terrain chunk game object.
        /// </summary>
        public int WorldScale { get; set; }
    }
}