using STerrain.Common;
using STerrain.Meshers;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static UnityEngine.Mesh;

namespace STerrain.EndlessTerrain
{
    public struct GenerateTerrainMeshJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> VoxelData;

        public MeshDataArray MeshDataArray;

        public void Execute(int index)
        {
            const int flattenedArraySize = EndlessTerrain.NOISE_SIZE * EndlessTerrain.NOISE_SIZE * EndlessTerrain.NOISE_SIZE;
            var voxelDataSlice = new NativeSlice<float>(VoxelData, flattenedArraySize * index, flattenedArraySize);
            var terrainMeshData = new TransvoxelMesher(
                new VoxelData(voxelDataSlice.ToArray(), EndlessTerrain.NOISE_SIZE),
                EndlessTerrain.CHUNK_SIZE,
                CubeFaceDirection.None,
                Vector3Int.one).ExtractSurface();

            var meshData = MeshDataArray[index];
            MeshBuilder.Build(meshData, terrainMeshData.Vertices, terrainMeshData.Normals, terrainMeshData.Triangles);
        }
    }
}