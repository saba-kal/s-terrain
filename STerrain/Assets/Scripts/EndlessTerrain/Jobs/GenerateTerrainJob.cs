using STerrain.Common;
using STerrain.Meshers;
using STerrain.Noise;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace STerrain.EndlessTerrain
{
    public struct GenerateTerrainJob : IJobParallelFor
    {
        public GenerateTerrainJobData JobData;

        public void Execute(int index)
        {
            VoxelData voxelData;
            if (JobData.NeedsVoxelData[index])
            {
                voxelData = GenerateVoxelData(index);
            }
            else
            {
                voxelData = VoxelData.ExtractFromNativeArray(JobData.ResultVoxelsArray, index, JobData.NoiseSettings.Size);
            }

            GenerateMesh(index, voxelData);
        }

        private VoxelData GenerateVoxelData(int index)
        {
            var bounds = JobData.BoundsArray[index];
            var position = bounds.center - (bounds.size / 2f);
            var scale = Mathf.FloorToInt(bounds.size.x / JobData.ChunkSize);
            var sampleCenter = position / scale - Vector3.one;

            var voxelData = TerrainNoise.Generate(
                JobData.NoiseSettings.Size,
                JobData.NoiseSettings.Size,
                JobData.NoiseSettings.Size,
                scale,
                sampleCenter,
                JobData.NoiseSettings);

            voxelData.AppendToNativeArray(JobData.ResultVoxelsArray, index);

            return voxelData;
        }

        private void GenerateMesh(int index, VoxelData voxelData)
        {
            var flattenedArraySize = JobData.NoiseSettings.Size * JobData.NoiseSettings.Size * JobData.NoiseSettings.Size;
            var voxelDataSlice = new NativeSlice<float>(JobData.ResultVoxelsArray, flattenedArraySize * index, flattenedArraySize);
            voxelDataSlice.CopyFrom(voxelData.ToArray());

            var terrainMeshData = new TransvoxelMesher(
                voxelData,
                JobData.ChunkSize,
                JobData.LodTransitionConfigArray[index],
                Vector3Int.one).ExtractSurface();

            var meshData = JobData.ResultMeshDataArray[index];
            MeshBuilder.Build(meshData, terrainMeshData.Vertices, terrainMeshData.Normals, terrainMeshData.Triangles);
        }
    }
}