using STerrain.Common;
using STerrain.Settings;
using System.Collections;
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
        [SerializeField] private Transform _viewer;
        [SerializeField] private float _viewerMoveThresholdForChunkUpdate = 25f;
        [SerializeField] private int _renderDistanceMultiplier = 31;
        [SerializeField] private int _octreeDivisionCount = 8;
        [SerializeField] private TerrainSettings _terrainSettings;

        private EndlessTerrainJobScheduler _scheduler;
        private Transform _transform;
        private float _sqrViewerMoveThresholdForChunkUpdate;
        private Vector3 _viewerPositionOld;
        private Octree _octree;
        private Dictionary<Bounds, TerrainChunk> _terrainChunkDictionary = new Dictionary<Bounds, TerrainChunk>();
        private HashSet<Bounds> _visibleChunksBounds = new HashSet<Bounds>();
        private List<TerrainChunk> _chunksThatNeedUpdate = new List<TerrainChunk>();
        private bool _terrainGenerationWasScheduled = false;

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
            if ((_viewerPositionOld - _viewer.position).sqrMagnitude > _sqrViewerMoveThresholdForChunkUpdate &&
                !_terrainGenerationWasScheduled)
            {
                _viewerPositionOld = _viewer.position;
                UpdateVisibleChunks();
            }
        }

        private void UpdateVisibleChunks()
        {
            var chunkSize = _terrainSettings.ChunkSize;
            var currentChunkPosition = new Vector3(
                Mathf.RoundToInt(_viewer.position.x / chunkSize) * chunkSize,
                Mathf.RoundToInt(_viewer.position.y / chunkSize) * chunkSize,
                Mathf.RoundToInt(_viewer.position.z / chunkSize) * chunkSize);
            var terrainBounds = new Bounds(currentChunkPosition, Vector3.one * chunkSize * _renderDistanceMultiplier);

            _octree = new Octree(terrainBounds, _octreeDivisionCount);
            _octree.Insert(currentChunkPosition);

            GetChunksThatNeedToUpdate();
            ScheduleChunkUpdates();
            StartCoroutine(ProcessGeneratedChunks());
        }

        private void GetChunksThatNeedToUpdate()
        {
            _visibleChunksBounds = new HashSet<Bounds>();
            _chunksThatNeedUpdate = new List<TerrainChunk>();
            foreach (var bound in _octree.GetAllLeafBounds())
            {
                var lodTransitionConfiguration = GetNeighborChunksWithLowerLod(bound);

                if (_terrainChunkDictionary.TryGetValue(bound, out var terrainChunk))
                {
                    terrainChunk.TryReuseMeshWithLodTransitionConfiguration(lodTransitionConfiguration);
                    if (terrainChunk.NeedsMeshUpdate)
                    {
                        _chunksThatNeedUpdate.Add(terrainChunk);
                    }
                    else
                    {
                        terrainChunk.SetVisible(true);
                    }
                }
                else
                {
                    var newChunk = new TerrainChunk(bound, lodTransitionConfiguration);
                    _chunksThatNeedUpdate.Add(newChunk);
                    _terrainChunkDictionary.Add(bound, newChunk);
                }

                _visibleChunksBounds.Add(bound);
            }
        }

        private void ScheduleChunkUpdates()
        {
            _scheduler.ScheduleChunkGenerationJob(_chunksThatNeedUpdate, _terrainSettings);
            _terrainGenerationWasScheduled = true;
        }

        private IEnumerator ProcessGeneratedChunks()
        {
            yield return new WaitUntil(_scheduler.IsComplete);

            HideChunksThatAreNotVisible();

            var terrainGenerationResult = _scheduler.CompleteJob();
            foreach (var result in terrainGenerationResult)
            {
                var chunk = _terrainChunkDictionary[result.Bounds];
                if (chunk.NeedsToBuild)
                {
                    var scale = Mathf.FloorToInt(chunk.Bounds.size.x / _terrainSettings.ChunkSize);
                    var position = chunk.Bounds.center - (chunk.Bounds.size / 2) - Vector3.one * scale;

                    chunk.VoxelData = result.VoxelData;
                    chunk.SetMesh(result.Mesh, result.LodTransitionConfig);
                    chunk.BuildChunk(new TerrainChunkParams
                    {
                        Parent = _transform,
                        Material = _terrainSettings.TerrainMaterial,
                        Position = position,
                        Scale = scale
                    });
                }
                else if (chunk.NeedsMeshUpdate)
                {
                    chunk.SetMesh(result.Mesh, result.LodTransitionConfig);
                    chunk.ReBuild();
                    chunk.SetVisible(true);
                }
            }
            _terrainGenerationWasScheduled = false;

            yield return null;
        }

        private void HideChunksThatAreNotVisible()
        {
            foreach (var chunk in _terrainChunkDictionary.Values)
            {
                if (!_visibleChunksBounds.Contains(chunk.Bounds))
                {
                    chunk.SetVisible(false);
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