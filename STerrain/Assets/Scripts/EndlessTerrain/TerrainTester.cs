using STerrain.Common;
using STerrain.Meshers;
using STerrain.Noise;
using UnityEngine;

namespace STerrain.EndlessTerrain
{
    public class TerrainTester : MonoBehaviour
    {
        [Header("Noise Settings")]
        [SerializeField] private float _scale;
        [SerializeField] private int _octaves;
        [Range(0, 1)]
        [SerializeField] private float _persistence;
        [SerializeField] private float _lacunarity;
        [SerializeField] private int _seed;
        [SerializeField] private Vector3 _offset;

        [Header("Player Settings")]
        [SerializeField] private Transform _viewer;
        [SerializeField] private float _viewerMoveThresholdForChunkUpdate = 25f;
        [SerializeField] private int _renderDistanceMultiplier = 16;
        [SerializeField] private Renderer _textureRenderer;

        [Header("Terrain Settings")]
        [SerializeField] private Material _terrainMaterial;
        [SerializeField] private int _worldScale;

        // Use this for initialization
        private void Start()
        {
            var voxelData = TerrainNoise.Generate(
                EndlessTerrain.NOISE_SIZE,
                EndlessTerrain.NOISE_SIZE,
                EndlessTerrain.NOISE_SIZE,
                1,
                Vector3.zero,
                new NoiseSettings
                {
                    Scale = _scale,
                    Octaves = _octaves,
                    Persistence = _persistence,
                    Lacunarity = _lacunarity,
                    Seed = _seed,
                    Offset = _offset
                });

            var terrainMeshData = new TransvoxelMesher(
                voxelData,
                EndlessTerrain.CHUNK_SIZE,
                CubeFaceDirection.None,
                Vector3Int.one).ExtractSurface();

            var mesh = new Mesh();
            mesh.vertices = terrainMeshData.Vertices.ToArray();
            mesh.normals = terrainMeshData.Normals.ToArray();
            mesh.triangles = terrainMeshData.Triangles.ToArray();

            var terrainChunk = new TerrainChunk(new Bounds(Vector3.zero, Vector3.one * 16), CubeFaceDirection.None);
            terrainChunk.VoxelData = voxelData;
            terrainChunk.SetMesh(mesh);
            terrainChunk.BuildChunk(new TerrainChunkParams
            {
                Parent = transform,
                Material = _terrainMaterial,
                WorldScale = _worldScale
            });

            CreateTexture(voxelData);
        }

        private void CreateTexture(VoxelData voxelData)
        {
            DrawTexture(TextureFromNoiseMap(voxelData));
        }

        private void DrawTexture(Texture2D texture)
        {
            _textureRenderer.sharedMaterial.mainTexture = texture;
            _textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;
        }

        public static Texture2D TextureFromNoiseMap(VoxelData voxelData)
        {
            var colorMap = new Color[EndlessTerrain.NOISE_SIZE * EndlessTerrain.NOISE_SIZE];
            for (var z = 0; z < EndlessTerrain.NOISE_SIZE; z++)
            {
                for (var y = 0; y < EndlessTerrain.NOISE_SIZE; y++)
                {
                    for (var x = 0; x < EndlessTerrain.NOISE_SIZE; x++)
                    {
                        colorMap[y * EndlessTerrain.NOISE_SIZE + x] = Color.Lerp(
                            Color.black,
                            Color.white,
                            Mathf.InverseLerp(-0.1f, 0.1f, voxelData[x, y, z]));
                    }
                }
            }

            return TextureFromColourMap(colorMap, EndlessTerrain.NOISE_SIZE, EndlessTerrain.NOISE_SIZE);
        }

        public static Texture2D TextureFromColourMap(Color[] colorMap, int width, int height)
        {
            var texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(colorMap);
            texture.Apply();
            return texture;
        }
    }
}