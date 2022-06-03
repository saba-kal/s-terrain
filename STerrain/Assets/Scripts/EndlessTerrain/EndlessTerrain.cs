using STerrain.Common;
using System.Collections.Generic;
using UnityEngine;

namespace STerrain.EndlessTerrain
{
    /// <summary>
    /// Manager for the entire endless terrain feature.
    /// </summary>
    [RequireComponent(typeof(EndlessTerrainJobScheduler))]
    public class EndlessTerrain : MonoBehaviour
    {
        public const int NOISE_SIZE = 19;
        public const int CHUNK_SIZE = 16;

        [Header("Noise Settings")]
        [SerializeField] private float _scale;
        [SerializeField] private int _octaves;
        [Range(0, 1)]
        [SerializeField] private float _persistence;
        [SerializeField] private float _lacunarity;
        [SerializeField] private int _seed;
        [SerializeField] private Vector3 _offset;

        [Header("Player Settings")]
        [SerializeField] private Transform _viewer;
        [SerializeField] private float _viewerMoveThresholdForChunkUpdate = 25f;
        [SerializeField] private int _renderDistanceMultiplier = 16;

        [Header("Terrain Settings")]
        [SerializeField] private Material _terrainMaterial;
        [SerializeField] private int _worldScale;

        private EndlessTerrainJobScheduler _scheduler;
        private Transform _transform;
        private float _sqrViewerMoveThresholdForChunkUpdate;
        private Vector3 _viewerPositionOld;
        private Octree _octree;
        private Dictionary<Bounds, TerrainChunk> _terrainChunkDictionary = new Dictionary<Bounds, TerrainChunk>();

        private void Awake()
        {
            _scheduler = GetComponent<EndlessTerrainJobScheduler>();
            _transform = transform;
        }

        private void Start()
        {
            _sqrViewerMoveThresholdForChunkUpdate = _viewerMoveThresholdForChunkUpdate * _viewerMoveThresholdForChunkUpdate;
            UpdateVisibleChunks();
        }

        private void Update()
        {
            if ((_viewerPositionOld - _viewer.position).sqrMagnitude > _sqrViewerMoveThresholdForChunkUpdate)
            {
                _viewerPositionOld = _viewer.position;
                UpdateVisibleChunks();
            }
        }

        private void UpdateVisibleChunks()
        {
            var currentChunkPosition = new Vector3(
                Mathf.RoundToInt(_viewer.position.x / CHUNK_SIZE) * CHUNK_SIZE,
                Mathf.RoundToInt(_viewer.position.y / CHUNK_SIZE) * CHUNK_SIZE,
                Mathf.RoundToInt(_viewer.position.z / CHUNK_SIZE) * CHUNK_SIZE);

            _octree = new Octree(
                new Bounds(currentChunkPosition, Vector3.one * CHUNK_SIZE * _renderDistanceMultiplier),
                CHUNK_SIZE);
            _octree.Insert(_viewer.position);

            var visibleChunkBounds = new HashSet<Bounds>();
            var chunksThatNeedUpdate = new List<TerrainChunk>();
            foreach (var bound in _octree.GetAllLeafBounds())
            {
                var lodTransitionConfiguration = GetNeighborChunksWithLowerLod(bound);

                if (_terrainChunkDictionary.TryGetValue(bound, out var terrainChunk))
                {
                    //Terrain chunk was previously visited.
                    if (terrainChunk.TryReuseMeshWithLodTransitionConfiguration(lodTransitionConfiguration))
                    {
                        terrainChunk.SetVisible(true);
                    }
                    else
                    {
                        terrainChunk.CurrentLodTransitionConfig = lodTransitionConfiguration;
                        chunksThatNeedUpdate.Add(terrainChunk);
                    }
                }
                else
                {
                    var newChunk = new TerrainChunk(bound, lodTransitionConfiguration);
                    chunksThatNeedUpdate.Add(newChunk);
                    _terrainChunkDictionary.Add(bound, newChunk);
                }

                visibleChunkBounds.Add(bound);
            }

            HideChunksThatAreNotVisible(visibleChunkBounds);
            ScheduleChunkUpdates(chunksThatNeedUpdate);
        }

        private void HideChunksThatAreNotVisible(HashSet<Bounds> visibleChunkBounds)
        {
            foreach (var chunk in _terrainChunkDictionary.Values)
            {
                if (!visibleChunkBounds.Contains(chunk.Bounds))
                {
                    chunk.SetVisible(false);
                }
            }
        }

        private void ScheduleChunkUpdates(List<TerrainChunk> chunksThatNeedUpdate)
        {
            var noiseSettings = new Noise.NoiseSettings
            {
                Scale = _scale,
                Octaves = _octaves,
                Persistence = _persistence,
                Lacunarity = _lacunarity,
                Seed = _seed,
                Offset = _offset
            };

            _scheduler.ScheduleChunkGenerationJob(chunksThatNeedUpdate, noiseSettings);
            foreach (var result in _scheduler.CompleteJob())
            {
                var chunk = _terrainChunkDictionary[result.Bounds];
                if (chunk.VoxelData != null)
                {
                    chunk.SetMesh(result.Mesh);
                }
                else
                {
                    chunk.VoxelData = result.VoxelData;
                    chunk.SetMesh(result.Mesh);
                    chunk.BuildChunk(new TerrainChunkParams
                    {
                        Parent = _transform,
                        Material = _terrainMaterial,
                        WorldScale = _worldScale
                    });
                }
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
    }
}