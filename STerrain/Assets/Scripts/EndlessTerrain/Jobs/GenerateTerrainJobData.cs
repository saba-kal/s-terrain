using STerrain.Common;
using STerrain.Noise;
using System;
using Unity.Collections;
using UnityEngine;
using static UnityEngine.Mesh;

namespace STerrain.EndlessTerrain
{
    public struct GenerateTerrainJobData : IDisposable
    {
        [ReadOnly] public NativeArray<Bounds> BoundsArray;
        [ReadOnly] public NativeArray<CubeFaceDirection> LodTransitionConfigArray;
        [ReadOnly] public NativeArray<bool> NeedsVoxelData;
        [ReadOnly] public int ChunkSize;
        [ReadOnly] public NoiseSettings NoiseSettings;

        [NativeDisableParallelForRestriction] public NativeArray<float> ResultVoxelsArray;
        public MeshDataArray ResultMeshDataArray;

        public void Dispose()
        {
            BoundsArray.Dispose();
            LodTransitionConfigArray.Dispose();
            NeedsVoxelData.Dispose();
            ResultVoxelsArray.Dispose();
        }
    }
}