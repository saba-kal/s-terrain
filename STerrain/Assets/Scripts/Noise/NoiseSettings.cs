using UnityEngine;

namespace STerrain.Noise
{
    /// <summary>
    /// Holds parameters for generating 3D noise.
    /// </summary>
    [SerializeField]
    public struct NoiseSettings
    {
        /// <summary>
        /// Scale of the noise. Increasing this will increase the width of terrain features.
        /// </summary>
        public float Scale;

        /// <summary>
        /// Number of noise layers to use when generating the noise.
        /// </summary>
        public int Octaves;

        /// <summary>
        /// Decrease in amplitude of each subsequent octave. Value must be between 0 and 1.
        /// amplitude = persistence ^ octave
        /// </summary>
        [Range(0, 1)]
        public float Persistence;

        /// <summary>
        /// Increase in frequency of each subsequent octave.
        /// frequency = lacunarity ^ octave
        /// </summary>
        public float Lacunarity;

        /// <summary>
        /// Seed used for the random number generator.
        /// </summary>
        public int Seed;

        /// <summary>
        /// The location offset of the noise.
        /// </summary>
        public Vector3 Offset;
    }
}