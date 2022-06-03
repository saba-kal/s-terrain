using STerrain.Noise;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace STerrain.EndlessTerrain
{
    public struct GenerateVoxelDataJob : IJobParallelFor
    {
        [ReadOnly] public NoiseSettings NoiseSettings;
        [ReadOnly] public NativeArray<TerrainChunkSettings> TerrainSettings;

        [NativeDisableParallelForRestriction]
        public NativeArray<float> VoxelData;

        public void Execute(int index)
        {
            var bounds = TerrainSettings[index].Bounds;
            var position = bounds.center - (bounds.size / 2f);
            var scale = Mathf.FloorToInt(bounds.size.x / EndlessTerrain.CHUNK_SIZE);
            var sampleCenter = position / scale;

            var voxelData = TerrainNoise.Generate(
                EndlessTerrain.NOISE_SIZE,
                EndlessTerrain.NOISE_SIZE,
                EndlessTerrain.NOISE_SIZE,
                scale,
                sampleCenter,
                NoiseSettings);

            const int flattenedArraySize = EndlessTerrain.NOISE_SIZE * EndlessTerrain.NOISE_SIZE * EndlessTerrain.NOISE_SIZE;
            var voxelDataSlice = new NativeSlice<float>(VoxelData, flattenedArraySize * index, flattenedArraySize);
            voxelDataSlice.CopyFrom(voxelData.ToNativeArray());
        }
    }
}