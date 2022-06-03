using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Mesh;

namespace STerrain.Common
{
    /// <summary>
    /// Mesh builder that is allowed to be called from a Unity job thread.
    /// </summary>
    public static class MeshBuilder
    {
        /// <summary>
        /// Builds a mesh.
        /// </summary>
        /// <param name="meshData">The mesh data object to store the mesh in.</param>
        /// <param name="vertices">List of mesh vertices.</param>
        /// <param name="normals">List of mesh normals.</param>
        /// <param name="triangles">List of mesh triangles.</param>
        public static void Build(
            MeshData meshData,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<int> triangles)
        {
            meshData.SetVertexBufferParams(vertices.Count,
                new VertexAttributeDescriptor(VertexAttribute.Position, stream: 0),
                new VertexAttributeDescriptor(VertexAttribute.Normal, stream: 1));
            meshData.SetIndexBufferParams(triangles.Count, IndexFormat.UInt32);

            var outputVertices = meshData.GetVertexData<Vector3>(stream: 0);
            outputVertices.CopyFrom(vertices.ToArray());

            var outputNormals = meshData.GetVertexData<Vector3>(stream: 1);
            outputNormals.CopyFrom(normals.ToArray());

            var outputTriangles = meshData.GetIndexData<int>();
            outputTriangles.CopyFrom(triangles.ToArray());

            meshData.subMeshCount = 1;
            SubMeshDescriptor subMeshDescriptor = new SubMeshDescriptor()
            {
                indexCount = triangles.Count,
                topology = MeshTopology.Triangles,
                vertexCount = vertices.Count,
                bounds = default,
            };

            meshData.SetSubMesh(0, subMeshDescriptor);
        }
    }
}