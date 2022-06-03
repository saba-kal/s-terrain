using System.Collections.Generic;
using UnityEngine;

namespace STerrain.Meshers
{
    /// <summary>
    /// Holds data needed for creating a terrain mesh.
    /// </summary>
    public class TerrainMeshData
    {
        /// <summary>
        /// List of vertices belonging to the mesh.
        /// </summary>
        public List<Vector3> Vertices { get; set; }

        /// <summary>
        /// List of normals belonging to the mesh.
        /// </summary>
        public List<Vector3> Normals { get; set; }

        /// <summary>
        /// List vertex indices that indicate triangle layout.
        /// </summary>
        public List<int> Triangles { get; set; }

        public TerrainMeshData()
        {
            Vertices = new List<Vector3>();
            Normals = new List<Vector3>();
            Triangles = new List<int>();
        }
    }
}