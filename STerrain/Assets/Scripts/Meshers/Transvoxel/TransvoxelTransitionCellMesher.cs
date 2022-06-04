using STerrain.Common;
using STerrain.EndlessTerrain;
using UnityEngine;

namespace STerrain.Meshers
{
    public class TransvoxelTransitionCellMesher
    {
        private readonly VoxelData _voxelData;
        private readonly int _size;
        private readonly CubeFaceDirection _chunkFacesWithLowerLod;
        private readonly Vector3Int _min;
        private readonly VertexCache _vertexCache;
        private readonly TerrainMeshData _meshData;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="voxelData">
        /// 3D array of floats indicating which points are inside and outside of the terrain. 
        /// Positive values are treated as outside. Negative values are treated as inside.
        /// </param>
        /// <param name="size">THe one-dimensional size of the voxel data.</param>
        /// <param name="chunkFacesWithLowerLod">Bit mask of chunk faces that transition to lower LOD.</param>
        /// <param name="min">Minimum position offset of each vertex.</param>
        /// <param name="vertexCache">Class for caching generated vertices.</param>
        /// <param name="meshData">Place to store vertices, normals, and triangles.</param>
        public TransvoxelTransitionCellMesher(
            VoxelData voxelData,
            int size,
            CubeFaceDirection chunkFacesWithLowerLod,
            Vector3Int min,
            VertexCache vertexCache,
            TerrainMeshData meshData)
        {
            _voxelData = voxelData;
            _size = size;
            _chunkFacesWithLowerLod = chunkFacesWithLowerLod;
            _min = min;
            _vertexCache = vertexCache;
            _meshData = meshData;
        }

        public void GenerateTransitionCell(Vector3Int pos, CubeFaceDirection direction)
        {
            var offsetPos = pos + _min;
            var cellSamples = GetTransitionCellSamples(offsetPos.x, offsetPos.y, offsetPos.z, direction);
            var caseCode = GetTransitionCaseCode(cellSamples);

            if (caseCode == 0 || caseCode == 511)
            {
                //Transition cell with case codes 0 and 511 contains no triangles.
                return;
            }

            var cellNormals = new Vector3[13];
            for (int i = 0; i < 9; i++)
            {
                var p = offsetPos + GetTransitionVertexIndices(direction)[i];
                float nx = (_voxelData[p.x + 1, p.y, p.z] - _voxelData[p.x - 1, p.y, p.z]) * 0.5f;
                float ny = (_voxelData[p.x, p.y + 1, p.z] - _voxelData[p.x, p.y - 1, p.z]) * 0.5f;
                float nz = (_voxelData[p.x, p.y, p.z + 1] - _voxelData[p.x, p.y, p.z - 1]) * 0.5f;
                cellNormals[i].x = nx;
                cellNormals[i].y = ny;
                cellNormals[i].z = nz;
                cellNormals[i].Normalize();
            }
            cellNormals[0x9] = cellNormals[0];
            cellNormals[0xA] = cellNormals[2];
            cellNormals[0xB] = cellNormals[6];
            cellNormals[0xC] = cellNormals[8];

            //High bit in cell class indicates whether we should flip the triangles.
            var cellClass = TransvoxelTables.transitionCellClass[caseCode];
            bool flipTriangles;
            if (direction == CubeFaceDirection.NegativeZ ||
                direction == CubeFaceDirection.PositiveX ||
                direction == CubeFaceDirection.PositiveY)
            {
                //No idea why I need to do this. If I don't, triangle will be inverted on certain faces.
                flipTriangles = (cellClass & 0b_1000_0000) == 0;
            }
            else
            {
                flipTriangles = (cellClass & 0b_1000_0000) != 0;
            }

            var cellData = TransvoxelTables.transitionCellData[cellClass & 0b_0111_1111];
            var vertexLocations = TransvoxelTables.transitionVertexData[caseCode];
            var vertexCount = cellData.GetVertexCount();
            var triangleCount = cellData.GetTriangleCount();
            var indexOffset = cellData.Indizes(); //index offsets for current cell
            var mappedIndices = new ushort[indexOffset.Length]; //array with real indizes for current cell

            for (var i = 0; i < vertexCount; i++)
            {
                // The low byte contains the indexes for the two endpoints of the edge on which the vertex lies,
                // as numbered in Figure 3.7. The high byte contains the vertex reuse data shown in Figure 3.8.
                int vertexLocation = vertexLocations[i];

                //The corner indexes are stored in the low and high nibbles (4-bit values) of the edge code byte.
                byte cornerIndexes = (byte)(vertexLocation & 0b_0000_0000_1111_1111);
                byte v1 = (byte)(cornerIndexes & 0b_0000_1111); //Second Corner Index
                byte v0 = (byte)(cornerIndexes >> 4); //First Corner Index

                //Get the noise values at the corresponding cell.
                var d0 = cellSamples[v0];
                var d1 = cellSamples[v1];

                //Interpolate between the values.
                float t = d1 / (d1 - d0);
                float t0 = t;
                float t1 = 1f - t;

                bool isFullResSide;
                if (t > 0 && t < 1)
                {
                    //Vertex lies in the interior edge
                    isFullResSide = v1 < 9 || v0 < 9;
                }
                else
                {
                    // The vertex is exactly on one of the edge endpoints.
                    var cellIndex = t == 0 ? v1 : v0;
                    isFullResSide = cellIndex < 9;
                }

                var normal = cellNormals[v0] * t0 + cellNormals[v1] * t1;
                //var vertexPosition = GetTransitionVertexPosition(pos, t, v0, v1);

                var p0i = offsetPos + GetTransitionVertexIndices(direction)[v0];
                var p0 = new Vector3(p0i.x, p0i.y, p0i.z);
                var p1i = offsetPos + GetTransitionVertexIndices(direction)[v1];
                var p1 = new Vector3(p1i.x, p1i.y, p1i.z);
                var vertex = p0 * t0 + p1 * t1;

                if (direction == CubeFaceDirection.NegativeZ)
                {
                    pos = new Vector3Int(pos.x, pos.y, pos.z);
                }

                if (isFullResSide &&
                    (TransvoxelCommon.PositionShouldUseBePushedBack(p0i - _min, _size, _chunkFacesWithLowerLod) ||
                    TransvoxelCommon.PositionShouldUseBePushedBack(p1i - _min, _size, _chunkFacesWithLowerLod)))
                {
                    vertex = TransvoxelCommon.GetSecondaryVertexPosition(vertex, normal, _size);
                }

                var index = _vertexCache.GetCachedIndex(vertex);
                if (index == null)
                {
                    //We can't reuse the vertex.
                    _meshData.Vertices.Add(vertex);
                    _meshData.Normals.Add(normal);
                    index = _meshData.Vertices.Count - 1;
                    _vertexCache.StoreVertex(vertex, index.Value);
                }

                mappedIndices[i] = (ushort)index;
            }

            for (int t = 0; t < triangleCount; t++)
            {
                if (flipTriangles)
                {
                    _meshData.Triangles.Add(mappedIndices[cellData.Indizes()[t * 3 + 2]]);
                    _meshData.Triangles.Add(mappedIndices[cellData.Indizes()[t * 3 + 1]]);
                    _meshData.Triangles.Add(mappedIndices[cellData.Indizes()[t * 3]]);
                }
                else
                {
                    _meshData.Triangles.Add(mappedIndices[cellData.Indizes()[t * 3]]);
                    _meshData.Triangles.Add(mappedIndices[cellData.Indizes()[t * 3 + 1]]);
                    _meshData.Triangles.Add(mappedIndices[cellData.Indizes()[t * 3 + 2]]);
                }
            }
        }

        private float[] GetTransitionCellSamples(int x, int y, int z, CubeFaceDirection direction)
        {
            var cellData = new float[13];
            var cellIndices = GetTransitionVertexIndices(direction);
            for (int i = 0; i < 13; i++)
            {
                var offset = cellIndices[i];
                cellData[i] = _voxelData[x + offset.x, y + offset.y, z + offset.z];
            }

            return cellData;
        }

        //  6---7---8  B-------C
        //  |   |   |  |       |
        //  3---4---5  |       |
        //  |   |   |  |       |
        //  0---1---2  9-------A
        private static Vector3Int[] GetTransitionVertexIndices(
            CubeFaceDirection direction)
        {
            return new Vector3Int[13] {
                ConvertTransitionCellCoordToVoxelCoord(0, 0, 0, direction), //0
                ConvertTransitionCellCoordToVoxelCoord(1, 0, 0, direction), //1
                ConvertTransitionCellCoordToVoxelCoord(2, 0, 0, direction), //2
                ConvertTransitionCellCoordToVoxelCoord(0, 1, 0, direction), //3
                ConvertTransitionCellCoordToVoxelCoord(1, 1, 0, direction), //4
                ConvertTransitionCellCoordToVoxelCoord(2, 1, 0, direction), //5
                ConvertTransitionCellCoordToVoxelCoord(0, 2, 0, direction), //6
                ConvertTransitionCellCoordToVoxelCoord(1, 2, 0, direction), //7
                ConvertTransitionCellCoordToVoxelCoord(2, 2, 0, direction), //8
                ConvertTransitionCellCoordToVoxelCoord(0, 0, 0, direction), //9
                ConvertTransitionCellCoordToVoxelCoord(2, 0, 0, direction), //A
                ConvertTransitionCellCoordToVoxelCoord(0, 2, 0, direction), //B
                ConvertTransitionCellCoordToVoxelCoord(2, 2, 0, direction) //C
            };
        }

        private static Vector3Int ConvertTransitionCellCoordToVoxelCoord(
            int tx, int ty, int tz, CubeFaceDirection direction)
        {
            //Transition cell could be oriented in one of three different ways depending on
            //the chunk face it is located on.
            switch (direction)
            {
                case CubeFaceDirection.NegativeX:
                case CubeFaceDirection.PositiveX:
                    return new Vector3Int(tz, ty, tx);
                case CubeFaceDirection.NegativeY:
                case CubeFaceDirection.PositiveY:
                    return new Vector3Int(tx, tz, ty);
                case CubeFaceDirection.NegativeZ:
                case CubeFaceDirection.PositiveZ:
                default:
                    return new Vector3Int(tx, ty, tz);
            }
        }

        private static long GetTransitionCaseCode(float[] cellSamples)
        {
            //The sign bits of the eight corner sample values are concatenated to form the case index for a cell.
            //Figure 4.17
            return ((GetSignBit(cellSamples[0]) >> 8) & 0x01)
                | ((GetSignBit(cellSamples[1]) >> 7) & 0x02)
                | ((GetSignBit(cellSamples[2]) >> 6) & 0x04)
                | ((GetSignBit(cellSamples[3]) >> 5) & 0x80)
                | ((GetSignBit(cellSamples[4]) >> 4) & 0x100)
                | ((GetSignBit(cellSamples[5]) >> 3) & 0x08)
                | ((GetSignBit(cellSamples[6]) >> 2) & 0x40)
                | ((GetSignBit(cellSamples[7]) >> 1) & 0x20)
                | (GetSignBit(cellSamples[8]) & 0x10);
        }

        private static int GetSignBit(float value)
        {
            return value < 0 ? -1 : 0;
        }
    }
}