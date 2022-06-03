using STerrain.Common;
using STerrain.EndlessTerrain;
using UnityEngine;

namespace STerrain.Meshers
{
    /// <summary>
    /// Class for generating regular voxel mesh cells using the modified marching cubes algorithm from the Transvoxel paper.
    /// </summary>
    public class TransvoxelRegularCellMesher
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
        /// Flattened 3D array of floats indicating which points are inside and outside of the terrain. 
        /// Positive values are treated as outside. Negative values are treated as inside.
        /// </param>
        /// <param name="size">THe one-dimensional size of the voxel data.</param>
        /// <param name="chunkFacesWithLowerLod">Bit mask of chunk faces that transition to lower LOD.</param>
        /// <param name="min">Minimum position offset of each vertex.</param>
        /// <param name="vertexCache">Class for caching generated vertices.</param>
        /// <param name="meshData">Place to store vertices, normals, and triangles.</param>
        public TransvoxelRegularCellMesher(
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

        /// <summary>
        /// Generates a voxel mesh cell at a given position in the voxel data.
        /// </summary>
        /// <param name="pos">Position in the voxel data.</param>
        public void GenerateRegularCell(
            Vector3Int pos)
        {
            var offsetPos = pos + _min;
            var density = GetCellCorners(offsetPos.x, offsetPos.y, offsetPos.z);
            var caseCode = GetCaseCode(density);
            if (caseCode == 0 || caseCode == 255)
            {
                //Cell with case codes 0 and 255 contains no triangles.
                return;
            }

            var cornerNormals = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                var p = offsetPos + cornerIndex[i];
                float nx = (_voxelData[p.x + 1, p.y, p.z] - _voxelData[p.x - 1, p.y, p.z]) * 0.5f;
                float ny = (_voxelData[p.x, p.y + 1, p.z] - _voxelData[p.x, p.y - 1, p.z]) * 0.5f;
                float nz = (_voxelData[p.x, p.y, p.z + 1] - _voxelData[p.x, p.y, p.z - 1]) * 0.5f;
                cornerNormals[i].x = nx;
                cornerNormals[i].y = ny;
                cornerNormals[i].z = nz;
                cornerNormals[i].Normalize();
            }

            var cellClass = TransvoxelTables.regularCellClass[caseCode];
            var cellData = TransvoxelTables.regularCellData[cellClass];
            var vertexLocations = TransvoxelTables.regularVertexData[caseCode];
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

                //Get the noise values at the corresponding cell corners.
                var d0 = density[v0];
                var d1 = density[v1];

                //Interpolate between the values.
                float t = d1 / (d1 - d0);
                float t0 = t;
                float t1 = 1f - t;

                var p0i = offsetPos + cornerIndex[v0];
                var p0 = new Vector3(p0i.x, p0i.y, p0i.z);
                var p1i = offsetPos + cornerIndex[v1];
                var p1 = new Vector3(p1i.x, p1i.y, p1i.z);
                var vertex = p0 * t0 + p1 * t1;
                var normal = cornerNormals[v0] * t0 + cornerNormals[v1] * t1;

                if (TransvoxelCommon.PositionShouldUseBePushedBack(p0i - _min, _size, _chunkFacesWithLowerLod) ||
                    TransvoxelCommon.PositionShouldUseBePushedBack(p1i - _min, _size, _chunkFacesWithLowerLod))
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
                for (int i = 0; i < 3; i++)
                {
                    _meshData.Triangles.Add(mappedIndices[cellData.Indizes()[t * 3 + i]]);
                }
            }
        }

        //   6--------7
        //  /|       /|
        // / |      / |
        //4--------5  |
        //|  2-----|--3
        //| /      | /
        //|/       |/
        //0--------1
        private float[] GetCellCorners(int x, int y, int z)
        {
            //Figure 3.7 in https://www.transvoxel.org/Lengyel-VoxelTerrain.pdf
            return new float[8]
            {
                _voxelData[x, y, z],
                _voxelData[x + 1, y, z],
                _voxelData[x, y + 1, z],
                _voxelData[x + 1, y + 1, z],
                _voxelData[x, y, z + 1],
                _voxelData[x + 1, y, z + 1],
                _voxelData[x, y + 1, z + 1],
                _voxelData[x + 1, y + 1, z + 1]
            };
        }

        //   6--------7
        //  /|       /|
        // / |      / |
        //4--------5  |
        //|  2-----|--3
        //| /      | /
        //|/       |/
        //0--------1
        private static readonly Vector3Int[] cornerIndex = new Vector3Int[8] {
            new Vector3Int(0, 0, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(1, 1, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(1, 0, 1),
            new Vector3Int(0, 1, 1),
            new Vector3Int(1, 1, 1)
        };

        private static long GetCaseCode(float[] cellSamples)
        {
            //The sign bits of the eight corner sample values are concatenated to form the case index for a cell.
            //Listing 3.1
            return ((GetSignBit(cellSamples[0]) >> 7) & 0x01)
                | ((GetSignBit(cellSamples[1]) >> 6) & 0x02)
                | ((GetSignBit(cellSamples[2]) >> 5) & 0x04)
                | ((GetSignBit(cellSamples[3]) >> 4) & 0x08)
                | ((GetSignBit(cellSamples[4]) >> 3) & 0x10)
                | ((GetSignBit(cellSamples[5]) >> 2) & 0x20)
                | ((GetSignBit(cellSamples[6]) >> 1) & 0x40)
                | (GetSignBit(cellSamples[7]) & 0x80);
        }

        private static int GetSignBit(float value)
        {
            return value < 0 ? -1 : 0;
        }
    }
}