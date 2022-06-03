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

        private readonly Octree _octree;
        private readonly Dictionary<CubeFaceDirection, Mesh> _availableLodTransitionConfigs;

        private Mesh _mesh;
        private GameObject _meshObject;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;

        public TerrainChunk(
            Bounds bounds, CubeFaceDirection neighborChunksWithLowerLod)
        {
            Bounds = bounds;
            CurrentLodTransitionConfig = neighborChunksWithLowerLod;
            _availableLodTransitionConfigs = new Dictionary<CubeFaceDirection, Mesh>();
        }

        /// <summary>
        /// Sets the mesh for this terrain chunk.
        /// </summary>
        /// <param name="mesh">The mesh to set.</param>
        public void SetMesh(Mesh mesh)
        {
            _mesh = mesh;
            _availableLodTransitionConfigs[CurrentLodTransitionConfig] = mesh;
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

            var position = Bounds.center - (Bounds.size / 2);
            var scale = Mathf.FloorToInt(Bounds.size.x / EndlessTerrain.CHUNK_SIZE);

            _meshObject = new GameObject("Terrain chunk");
            _meshObject.transform.position = position - Vector3.one * scale;
            _meshObject.transform.parent = parameters.Parent;
            _meshObject.transform.localScale = Vector3.one * scale;

            _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
            _meshRenderer.material = parameters.Material;

            _meshFilter = _meshObject.AddComponent<MeshFilter>();
            _meshFilter.mesh = _mesh;
            _meshFilter.mesh.RecalculateBounds();
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
        /// Gets the current LOD transition configuration for the generated mesh.
        /// </summary>
        public CubeFaceDirection GetLodTransitionConfiguration()
        {
            return CurrentLodTransitionConfig;
        }

        /// <summary>
        /// Attempts to re-use a mesh with the given LOD transition configuration.
        /// </summary>
        /// <param name="neighborChunksWithLowerLod">The LOD transition configuration to check.</param>
        /// <returns>True if re-use was successful. False if mesh does not exist for given configuration.</returns>
        public bool TryReuseMeshWithLodTransitionConfiguration(CubeFaceDirection neighborChunksWithLowerLod)
        {
            if (_availableLodTransitionConfigs.TryGetValue(neighborChunksWithLowerLod, out var mesh))
            {
                _meshFilter.mesh = mesh;
                return true;
            }

            return true;
        }

        private CubeFaceDirection GetNeighborChunksWithLowerLod()
        {
            var neighborChunksWithLowerLod = CubeFaceDirection.None;
            var translationAmount = Bounds.size.x;

            var rightBound = _octree.GetBound(Bounds.center + new Vector3(translationAmount, 0, 0));
            if (rightBound.HasValue && rightBound.Value.size.x > Bounds.size.x)
            {
                neighborChunksWithLowerLod |= CubeFaceDirection.PositiveX;
            }

            var leftBound = _octree.GetBound(Bounds.center + new Vector3(-translationAmount, 0, 0));
            if (leftBound.HasValue && leftBound.Value.size.x > Bounds.size.x)
            {
                neighborChunksWithLowerLod |= CubeFaceDirection.NegativeX;
            }

            var topBound = _octree.GetBound(Bounds.center + new Vector3(0, translationAmount, 0));
            if (topBound.HasValue && topBound.Value.size.x > Bounds.size.x)
            {
                neighborChunksWithLowerLod |= CubeFaceDirection.PositiveY;
            }

            var bottomBound = _octree.GetBound(Bounds.center + new Vector3(0, -translationAmount, 0));
            if (bottomBound.HasValue && bottomBound.Value.size.x > Bounds.size.x)
            {
                neighborChunksWithLowerLod |= CubeFaceDirection.NegativeY;
            }

            var forwardBound = _octree.GetBound(Bounds.center + new Vector3(0, 0, translationAmount));
            if (forwardBound.HasValue && forwardBound.Value.size.x > Bounds.size.x)
            {
                neighborChunksWithLowerLod |= CubeFaceDirection.PositiveZ;
            }

            var backwardBound = _octree.GetBound(Bounds.center + new Vector3(0, 0, -translationAmount));
            if (backwardBound.HasValue && backwardBound.Value.size.x > Bounds.size.x)
            {
                neighborChunksWithLowerLod |= CubeFaceDirection.NegativeZ;
            }

            return neighborChunksWithLowerLod;
        }
    }
}