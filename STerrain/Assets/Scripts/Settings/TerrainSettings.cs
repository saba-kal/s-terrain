using UnityEngine;

namespace STerrain.Settings
{
    /// <summary>
    /// Holds settings for generating terrains.
    /// </summary>
    [CreateAssetMenu(fileName = "TerrainSettings", menuName = "Scriptable Objects/Terrain Settings")]
    public class TerrainSettings : ScriptableObject
    {
        [Header("Terrain Settings")]
        [SerializeField] private Material _terrainMaterial;
        [SerializeField] private int _worldScale = 1;
        [SerializeField] private int _chunkSize = 16;

        [Header("Noise Settings")]
        [SerializeField] private float _noiseScale = 50;
        [SerializeField] private int _octaves = 6;
        [Range(0, 1)]
        [SerializeField] private float _persistence = 0.6f;
        [SerializeField] private float _lacunarity = 2;
        [SerializeField] private int _seed;
        [SerializeField] private Vector3 _offset;

        /// <summary>
        /// Material for the terrain mesh.
        /// </summary>
        public Material TerrainMaterial => _terrainMaterial;

        /// <summary>
        /// Scale of each terrain chunk game object.
        /// </summary>
        public int WorldScale => _worldScale;

        /// <summary>
        /// Size of each terrain chunk.
        /// </summary>
        public int ChunkSize => _chunkSize;

        /// <summary>
        /// Generating the triangle mesh for one block requires access to a volume of (chunkSize+3 x chunkSize+3 x chunkSize+3)
        /// voxels, where one layer of voxels precedes the negative boundaries of the block, and two layers of voxels succeed the
        /// positive boundaries of the block.
        /// </summary>
        public int NoiseSize => _chunkSize + 3;

        /// <summary>
        /// Scale of the noise. Increasing this will increase the width of terrain features.
        /// </summary>
        public float NoiseScale => _noiseScale;

        /// <summary>
        /// Number of noise layers to use when generating the noise map.
        /// </summary>
        public int Octaves => _octaves;

        /// <summary>
        /// Decrease in amplitude of each subsequent octave. Value must be between 0 and 1.
        /// amplitude = persistence ^ octave
        /// </summary>
        public float Persistence => _persistence;

        /// <summary>
        /// Increase in frequency of each subsequent octave.
        /// frequency = lacunarity ^ octave
        /// </summary>
        public float Lacunarity => _lacunarity;

        /// <summary>
        /// Seed used for the random number generator.
        /// </summary>
        public int Seed => _seed;

        /// <summary>
        /// The location offset of the noise.
        /// </summary>
        public Vector3 Offset => _offset;

        private void OnValidate()
        {
            _noiseScale = Mathf.Max(_noiseScale, 0.01f);
            _octaves = Mathf.Max(_octaves, 1);
            _lacunarity = Mathf.Max(_lacunarity, 1);
            _persistence = Mathf.Clamp01(_persistence);
        }
    }
}