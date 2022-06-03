using STerrain.Common;
using UnityEngine;

namespace STerrain.EndlessTerrain
{
    public class GenerateTerrainJobResult
    {
        public Bounds Bounds;
        public CubeFaceDirection LodTransitionConfig;
        public VoxelData VoxelData;
        public Mesh Mesh;
    }
}