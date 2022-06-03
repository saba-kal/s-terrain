using UnityEngine;

namespace STerrain.Common
{
    /// <summary>
    /// Contains extension methods for arrays.
    /// </summary>
    public static class ArrayExtensions
    {
        /// <summary>
        /// Gets a value from a flattened 3D array.
        /// </summary>
        public static T Get3D<T>(this T[] array, int i, int j, int k)
        {
            try
            {
                var size = array.Length / 3;
                return array[(k * size * size) + (j * size) + i];
            }
            catch
            {
                Debug.LogError($"Out of range: {i},{j},{k}");
                return default;
            }
        }
    }
}