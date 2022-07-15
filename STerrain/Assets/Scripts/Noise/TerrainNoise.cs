using STerrain.EndlessTerrain;
using UnityEngine;

namespace STerrain.Noise
{
    public static class TerrainNoise
    {
        /// <summary>
        /// Generates a 3D noise map.
        /// </summary>
        public static VoxelData Generate(
            int mapWidth,
            int mapHeight,
            int mapDepth,
            int scale,
            Vector3 sampleCenter,
            NoiseSettings noiseSettings)
        {
            var voxelData = new VoxelData(mapWidth, mapHeight, mapDepth);

            var offsetX = noiseSettings.Offset.x + sampleCenter.x;
            var offsetY = noiseSettings.Offset.y + sampleCenter.y;
            var offsetZ = noiseSettings.Offset.z + sampleCenter.z;

            var fastNoise = new FastNoiseLite(noiseSettings.Seed);
            fastNoise.SetFractalOctaves(noiseSettings.Octaves);
            fastNoise.SetFractalLacunarity(noiseSettings.Lacunarity);
            fastNoise.SetFractalGain(noiseSettings.Persistence);

            for (var x = 0; x < mapWidth; x++)
            {
                for (var y = 0; y < mapHeight; y++)
                {
                    for (var z = 0; z < mapDepth; z++)
                    {
                        var noiseLocation = new Vector3(
                            (x + offsetX) * scale * noiseSettings.Scale,
                            (y + offsetY) * scale * noiseSettings.Scale,
                            (z + offsetZ) * scale * noiseSettings.Scale);

                        fastNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);
                        fastNoise.SetFractalType(FastNoiseLite.FractalType.Ridged);
                        var terrainNoise = fastNoise.GetNoise(
                            noiseLocation.x,
                            noiseLocation.z);
                        //terrainNoise = terrainNoise * 0.5f + (noiseLocation.y / 50f);

                        //fastNoise.SetFrequency(0.05f);
                        fastNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
                        fastNoise.SetFractalType(FastNoiseLite.FractalType.DomainWarpProgressive);
                        var caveNoise = fastNoise.GetNoise(
                            noiseLocation.x,
                            noiseLocation.y,
                            noiseLocation.z);
                        caveNoise = (caveNoise * caveNoise * caveNoise) + 0.03f;


                        voxelData[x, y, z] = terrainNoise + caveNoise;
                    }
                }
            }

            return voxelData;
        }
    }
}