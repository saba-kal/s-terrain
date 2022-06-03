using STerrain.Common;
using STerrain.EndlessTerrain;
using UnityEngine;

namespace STerrain.Meshers
{
    /// <summary>
    /// Mesh generator using the transvoxel algorithm.
    /// </summary>
    public class TransvoxelMesher
    {
        private readonly int _size;
        private readonly CubeFaceDirection _chunkFacesWithLowerLod;
        private readonly TransvoxelRegularCellMesher _regularCellMesher;
        private readonly TransvoxelTransitionCellMesher _transitionCellMesher;
        private readonly TerrainMeshData _meshData;

        public TransvoxelMesher(
            VoxelData voxelData,
            int size,
            CubeFaceDirection chunkFacesWithLowerLod,
            Vector3Int min)
        {
            _size = size;
            _chunkFacesWithLowerLod = chunkFacesWithLowerLod;
            _meshData = new TerrainMeshData();

            var vertexCache = new VertexCache();

            _regularCellMesher = new TransvoxelRegularCellMesher(
                voxelData, size, chunkFacesWithLowerLod, min, vertexCache, _meshData);
            _transitionCellMesher = new TransvoxelTransitionCellMesher(
                voxelData, size, chunkFacesWithLowerLod, min, vertexCache, _meshData);
        }

        /// <summary>
        /// Extracts a mesh surface from the given voxel data using the Transvoxel algorithm.
        /// </summary>
        /// <returns>Object holding list of vertices, normals, and triangles of the mesh.</returns>
        public TerrainMeshData ExtractSurface()
        {
            //Generate regular cells.
            for (var x = 0; x < _size; x++)
            {
                for (var y = 0; y < _size; y++)
                {
                    for (var z = 0; z < _size; z++)
                    {
                        var pos = new Vector3Int(x, y, z);
                        _regularCellMesher.GenerateRegularCell(pos);
                    }
                }
            }

            //Loop through each face of the terrain chunk cube and generate transition cells
            //for faces that transition to higher LOD.
            for (var i = 0; i < 6; i++)
            {
                var direction = (CubeFaceDirection)(1 << i);
                if (!_chunkFacesWithLowerLod.HasFlag(direction))
                {
                    continue;
                }

                for (var fx = 0; fx < _size; fx += 2)
                {
                    for (var fy = 0; fy < _size; fy += 2)
                    {
                        var pos = ConvertChunkFaceCoordToVoxelCoord(fx, fy, direction);
                        _transitionCellMesher.GenerateTransitionCell(pos, direction);
                    }
                }
            }

            return _meshData;
        }

        private Vector3Int ConvertChunkFaceCoordToVoxelCoord(
            int fx, int fy, CubeFaceDirection direction)
        {
            switch (direction)
            {
                case CubeFaceDirection.None:
                    return new Vector3Int(0, 0, 0);
                case CubeFaceDirection.NegativeX:
                    return new Vector3Int(0, fy, fx);
                case CubeFaceDirection.PositiveX:
                    return new Vector3Int(_size, fy, fx);
                case CubeFaceDirection.NegativeY:
                    return new Vector3Int(fx, 0, fy);
                case CubeFaceDirection.PositiveY:
                    return new Vector3Int(fx, _size, fy);
                case CubeFaceDirection.NegativeZ:
                    return new Vector3Int(fx, fy, 0);
                case CubeFaceDirection.PositiveZ:
                    return new Vector3Int(fx, fy, _size);
            }

            return new Vector3Int(fx, fy, 0);
        }
    }
}