using System.Collections.Generic;
using UnityEngine;

namespace STerrain.Meshers
{
    /// <summary>
    /// Manages cache of vertices so that they can be re-used when generating the terrain mesh.
    /// TODO: figure out how to use the re-use indexes provided by the transvoxel tables. Storing
    /// every single vertex on the mesh in a dictionary is not very memory efficient.
    /// </summary>
    public class VertexCache
    {
        private readonly Dictionary<Vector3, int> _vertices2Indices = new Dictionary<Vector3, int>();

        /// <summary>
        /// Stores vertex position in memory.
        /// </summary>
        /// <param name="vertex">The vertex position to store.</param>
        /// <param name="index">Index of the vertex in the mesh.</param>
        public void StoreVertex(Vector3 vertex, int index)
        {
            if (!_vertices2Indices.ContainsKey(vertex))
            {
                _vertices2Indices.Add(vertex, index);
            }
        }

        /// <summary>
        /// Gets the index of a cached vertex. Returns null if vertex is not found.
        /// </summary>
        /// <param name="vertex">The vertex to get the index for.</param>
        public int? GetCachedIndex(Vector3 vertex)
        {
            if (_vertices2Indices.TryGetValue(vertex, out var index))
            {
                return index;
            }

            return null;
        }
    }
}