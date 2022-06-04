using STerrain.Common;
using STerrain.Meshers;
using STerrain.Noise;
using STerrain.Settings;
using UnityEngine;

namespace STerrain.EndlessTerrain
{
    public class TerrainPreview : MonoBehaviour
    {
        [SerializeField] private int _renderDistanceMultiplier = 16;
        [SerializeField] private TerrainSettings _terrainSettings;

        private Octree _octree;

        public void GenerateTerrain()
        {
            ClearExistingChunks();

            var chunkSize = _terrainSettings.ChunkSize;
            var currentChunkPosition = new Vector3(
                Mathf.RoundToInt(transform.position.x / chunkSize) * chunkSize,
                Mathf.RoundToInt(transform.position.y / chunkSize) * chunkSize,
                Mathf.RoundToInt(transform.position.z / chunkSize) * chunkSize);
            var terrainBounds = new Bounds(currentChunkPosition, Vector3.one * chunkSize * _renderDistanceMultiplier);

            _octree = new Octree(terrainBounds, chunkSize);
            _octree.Insert(transform.position);

            foreach (var bounds in _octree.GetAllLeafBounds())
            {
                var lodConfiguration = GetNeighborChunksWithLowerLod(bounds);
                var chunk = new TerrainChunk(bounds, lodConfiguration);

                var scale = Mathf.FloorToInt(bounds.size.x / _terrainSettings.ChunkSize);
                var position = bounds.center - (bounds.size / 2f) - Vector3.one * scale;
                var sampleCenter = (bounds.center - (bounds.size / 2f)) / scale;

                chunk.VoxelData = TerrainNoise.Generate(
                    _terrainSettings.NoiseSize,
                    _terrainSettings.NoiseSize,
                    _terrainSettings.NoiseSize,
                    scale,
                    sampleCenter - Vector3.one,
                    NoiseSettings.FromTerrainSettings(_terrainSettings));

                var terrainMeshData = new TransvoxelMesher(
                    chunk.VoxelData,
                    _terrainSettings.ChunkSize,
                    lodConfiguration,
                    Vector3Int.one).ExtractSurface();

                var mesh = new Mesh();
                mesh.vertices = terrainMeshData.Vertices.ToArray();
                mesh.normals = terrainMeshData.Normals.ToArray();
                mesh.triangles = terrainMeshData.Triangles.ToArray();

                chunk.SetMesh(mesh, lodConfiguration);

                chunk.BuildChunk(new TerrainChunkParams
                {
                    Parent = transform,
                    Material = _terrainSettings.TerrainMaterial,
                    Position = position,
                    Scale = scale
                });
            }
        }

        public void ClearExistingChunks()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                DestroyImmediate(child.gameObject);
            }
        }

        private CubeFaceDirection GetNeighborChunksWithLowerLod(Bounds chunkBounds)
        {
            var neighborChunksWithLowerLod = CubeFaceDirection.None;
            var translationAmount = chunkBounds.size.x;

            var rightBound = _octree.GetBound(chunkBounds.center + new Vector3(translationAmount, 0, 0));
            if (rightBound.HasValue && rightBound.Value.size.x > chunkBounds.size.x)
            {
                neighborChunksWithLowerLod |= CubeFaceDirection.PositiveX;
            }

            var leftBound = _octree.GetBound(chunkBounds.center + new Vector3(-translationAmount, 0, 0));
            if (leftBound.HasValue && leftBound.Value.size.x > chunkBounds.size.x)
            {
                neighborChunksWithLowerLod |= CubeFaceDirection.NegativeX;
            }

            var topBound = _octree.GetBound(chunkBounds.center + new Vector3(0, translationAmount, 0));
            if (topBound.HasValue && topBound.Value.size.x > chunkBounds.size.x)
            {
                neighborChunksWithLowerLod |= CubeFaceDirection.PositiveY;
            }

            var bottomBound = _octree.GetBound(chunkBounds.center + new Vector3(0, -translationAmount, 0));
            if (bottomBound.HasValue && bottomBound.Value.size.x > chunkBounds.size.x)
            {
                neighborChunksWithLowerLod |= CubeFaceDirection.NegativeY;
            }

            var forwardBound = _octree.GetBound(chunkBounds.center + new Vector3(0, 0, translationAmount));
            if (forwardBound.HasValue && forwardBound.Value.size.x > chunkBounds.size.x)
            {
                neighborChunksWithLowerLod |= CubeFaceDirection.PositiveZ;
            }

            var backwardBound = _octree.GetBound(chunkBounds.center + new Vector3(0, 0, -translationAmount));
            if (backwardBound.HasValue && backwardBound.Value.size.x > chunkBounds.size.x)
            {
                neighborChunksWithLowerLod |= CubeFaceDirection.NegativeZ;
            }

            return neighborChunksWithLowerLod;
        }

        private void OnDrawGizmos()
        {
            _octree?.DrawGizmo();
        }
    }
}