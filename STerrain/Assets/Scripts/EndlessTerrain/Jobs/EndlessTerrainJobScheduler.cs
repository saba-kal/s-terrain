using STerrain.Common;
using STerrain.Noise;
using STerrain.Settings;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static UnityEngine.Mesh;

namespace STerrain.EndlessTerrain
{
    /// <summary>
    /// Handles scheduling terrain chunk generation jobs.
    /// </summary>
    public class EndlessTerrainJobScheduler : MonoBehaviour
    {
        [SerializeField] private int _batchSize = 1;

        private JobHandle _generateTerrainHandle;
        private GenerateTerrainJobData _jobData;
        private int _jobCount = 0;
        private int _voxelDataSize;

        public void ScheduleChunkGenerationJob(List<TerrainChunk> terrainChunks, TerrainSettings terrainSettings)
        {
            _voxelDataSize = terrainSettings.NoiseSize;
            var voxelDataArraySize = _voxelDataSize * _voxelDataSize * _voxelDataSize;

            _jobCount = terrainChunks.Count;
            _jobData = new GenerateTerrainJobData()
            {
                BoundsArray = new NativeArray<Bounds>(_jobCount, Allocator.Persistent),
                LodTransitionConfigArray = new NativeArray<CubeFaceDirection>(_jobCount, Allocator.Persistent),
                NeedsVoxelData = new NativeArray<bool>(_jobCount, Allocator.Persistent),
                ChunkSize = terrainSettings.ChunkSize,
                NoiseSettings = NoiseSettings.FromTerrainSettings(terrainSettings),
                ResultVoxelsArray = new NativeArray<float>(_jobCount * voxelDataArraySize, Allocator.Persistent),
                ResultMeshDataArray = AllocateWritableMeshData(_jobCount)
            };

            for (var i = 0; i < _jobCount; i++)
            {
                _jobData.BoundsArray[i] = terrainChunks[i].Bounds;
                _jobData.LodTransitionConfigArray[i] = terrainChunks[i].CurrentLodTransitionConfig;
                if (terrainChunks[i].NeedsToBuild)
                {
                    _jobData.NeedsVoxelData[i] = true;
                }
                else
                {
                    _jobData.NeedsVoxelData[i] = false;
                    terrainChunks[i].VoxelData.AppendToNativeArray(_jobData.ResultVoxelsArray, i);
                }
            }

            var generateTerrainJob = new GenerateTerrainJob
            {
                JobData = _jobData
            };
            _generateTerrainHandle = generateTerrainJob.Schedule(_jobCount, _batchSize);
        }

        /// <summary>
        /// Completes all scheduled jobs and returns the result.
        /// </summary>
        public List<GenerateTerrainJobResult> CompleteJob()
        {
            _generateTerrainHandle.Complete();

            var result = new List<GenerateTerrainJobResult>();
            var meshes = new Mesh[_jobCount];
            for (var i = 0; i < _jobCount; i++)
            {
                meshes[i] = new Mesh();
                result.Add(new GenerateTerrainJobResult
                {
                    Bounds = _jobData.BoundsArray[i],
                    LodTransitionConfig = _jobData.LodTransitionConfigArray[i],
                    VoxelData = VoxelData.ExtractFromNativeArray(_jobData.ResultVoxelsArray, i, _voxelDataSize),
                    Mesh = meshes[i],
                });
            }

            ApplyAndDisposeWritableMeshData(_jobData.ResultMeshDataArray, meshes);

            _jobData.Dispose();

            return result;
        }

        public bool IsComplete()
        {
            return _generateTerrainHandle.IsCompleted;
        }
    }
}