using STerrain.Settings;
using UnityEngine;

namespace STerrain.Noise
{
    /// <summary>
    /// Holds parameters for generating 3D noise.
    /// Note: this is basically holds the same data as <see cref="TerrainSettings"/>.
    /// The reason it is needed is because only value types can be passed to Unity jobs.
    /// </summary>
    public struct NoiseSettings
    {
        public float Scale;
        public int Octaves;
        public float Persistence;
        public float Lacunarity;
        public int Seed;
        public Vector3 Offset;
        public int Size;

        public static NoiseSettings FromTerrainSettings(TerrainSettings terrainSettings)
        {
            return new NoiseSettings
            {
                Scale = terrainSettings.NoiseScale,
                Octaves = terrainSettings.Octaves,
                Persistence = terrainSettings.Persistence,
                Lacunarity = terrainSettings.Lacunarity,
                Seed = terrainSettings.Seed,
                Offset = terrainSettings.Offset,
                Size = terrainSettings.NoiseSize
            };
        }
    }
}