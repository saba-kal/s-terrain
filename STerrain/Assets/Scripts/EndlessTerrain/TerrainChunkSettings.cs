using STerrain.Common;
using UnityEngine;

namespace STerrain.EndlessTerrain
{
    public struct TerrainChunkSettings
    {
        public Bounds Bounds;
        public int WorldScale;
        public CubeFaceDirection ChunkFacesWithLowerLod;
    }
}