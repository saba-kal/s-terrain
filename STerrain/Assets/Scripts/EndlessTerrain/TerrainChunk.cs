using STerrain.Common;
using System.Collections.Generic;
using UnityEngine;

namespace STerrain.EndlessTerrain
{
    /// <summary>
    /// Represents a single terrain chunk.
    /// </summary>
    public class TerrainChunk
    {
        public Bounds Bounds { get; private set; }
        public VoxelData VoxelData { get; set; }
        public CubeFaceDirection CurrentLodTransitionConfig { get; set; }
        public bool NeedsToBuild => VoxelData == null;
        public bool NeedsMeshUpdate { get; set; }

        private readonly Dictionary<CubeFaceDirection, Mesh> _availableLodTransitionConfigs;
        private Mesh _mesh;
        private GameObject _meshObject;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;

        public TerrainChunk(Bounds bounds, CubeFaceDirection neighborChunksWithLowerLod)
        {
            Bounds = bounds;
            CurrentLodTransitionConfig = neighborChunksWithLowerLod;
            _availableLodTransitionConfigs = new Dictionary<CubeFaceDirection, Mesh>();
        }

        /// <summary>
        /// Builds the terrain chunk game object.
        /// </summary>
        public void BuildChunk(TerrainChunkParams parameters)
        {
            if (VoxelData == null)
            {
                throw new System.Exception($"Terrain chunk {Bounds} is missing voxel data.");
            }
            if (_mesh == null)
            {
                throw new System.Exception($"Terrain chunk {Bounds} is missing a mesh.");
            }

            _meshObject = new GameObject("Terrain chunk");
            _meshObject.transform.position = parameters.Position;
            _meshObject.transform.parent = parameters.Parent;
            _meshObject.transform.localScale = Vector3.one * parameters.Scale;

            _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
            _meshRenderer.material = parameters.Material;

            _meshFilter = _meshObject.AddComponent<MeshFilter>();
            _meshFilter.sharedMesh = _mesh;
            _meshFilter.sharedMesh.RecalculateBounds();

            if (Bounds.size.z == 16 && _mesh.vertexCount > 3)
            {
                _meshCollider = _meshObject.AddComponent<MeshCollider>();
                _meshCollider.sharedMesh = _mesh;
            }
        }

        /// <summary>
        /// Sets the current active mesh to the chunk mesh filter.
        /// </summary>
        public void SetMesh(Mesh mesh, CubeFaceDirection neighborChunksWithLowerLod)
        {
            _mesh = mesh;
            CurrentLodTransitionConfig = neighborChunksWithLowerLod;
            _availableLodTransitionConfigs[neighborChunksWithLowerLod] = mesh;
            NeedsMeshUpdate = false;
        }

        /// <summary>
        /// Rebuilds the terrain chunk mesh.
        /// </summary>
        public void ReBuild()
        {
            _meshFilter.sharedMesh = _mesh;
            _meshFilter.sharedMesh.RecalculateBounds();
        }

        /// <summary>
        /// Shows or hides this terrain chunk.
        /// </summary>
        /// <param name="visible">Whether or not the terrain chunk is visible.</param>
        public void SetVisible(bool visible)
        {
            _meshObject.SetActive(visible);
        }

        /// <summary>
        /// Attempts to re-use a mesh with the given LOD transition configuration.
        /// </summary>
        /// <param name="neighborChunksWithLowerLod">The LOD transition configuration to check.</param>
        /// <returns>True if re-use was successful. False if mesh does not exist for given configuration.</returns>
        public void TryReuseMeshWithLodTransitionConfiguration(CubeFaceDirection neighborChunksWithLowerLod)
        {
            CurrentLodTransitionConfig = neighborChunksWithLowerLod;
            if (_availableLodTransitionConfigs.TryGetValue(neighborChunksWithLowerLod, out var mesh))
            {
                _meshFilter.sharedMesh = mesh;
                NeedsMeshUpdate = false;
            }
            else
            {
                NeedsMeshUpdate = true;
            }

        }
    }
}