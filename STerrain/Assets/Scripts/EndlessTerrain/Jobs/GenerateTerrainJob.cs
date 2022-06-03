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
                voxelData = VoxelData.ExtractFromNativeArray(JobData.ResultVoxelsArray, index, EndlessTerrain.NOISE_SIZE);
            }

            GenerateMesh(index, voxelData);
        }

        private VoxelData GenerateVoxelData(int index)
        {
            var bounds = JobData.BoundsArray[index];
            var position = bounds.center - (bounds.size / 2f);
            var scale = Mathf.FloorToInt(bounds.size.x / EndlessTerrain.CHUNK_SIZE);
            var sampleCenter = position / scale;

            var voxelData = TerrainNoise.Generate(
                EndlessTerrain.NOISE_SIZE,
                EndlessTerrain.NOISE_SIZE,
                EndlessTerrain.NOISE_SIZE,
                scale,
                sampleCenter,
                JobData.NoiseSettings);

            voxelData.AppendToNativeArray(JobData.ResultVoxelsArray, index);

            return voxelData;
        }

        private void GenerateMesh(int index, VoxelData voxelData)
        {
            const int flattenedArraySize = EndlessTerrain.NOISE_SIZE * EndlessTerrain.NOISE_SIZE * EndlessTerrain.NOISE_SIZE;
            var voxelDataSlice = new NativeSlice<float>(JobData.ResultVoxelsArray, flattenedArraySize * index, flattenedArraySize);
            voxelDataSlice.CopyFrom(voxelData.ToArray());

            var terrainMeshData = new TransvoxelMesher(
                voxelData,
                EndlessTerrain.CHUNK_SIZE,
                JobData.LodTransitionConfigArray[index],
                Vector3Int.one).ExtractSurface();

            var meshData = JobData.ResultMeshDataArray[index];
            MeshBuilder.Build(meshData, terrainMeshData.Vertices, terrainMeshData.Normals, terrainMeshData.Triangles);
        }
    }
}