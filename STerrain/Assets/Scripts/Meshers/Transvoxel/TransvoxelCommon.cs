using STerrain.Common;
using UnityEngine;

namespace STerrain.Meshers
{
    /// <summary>
    /// Common functions used by the regular and transition cell meshers in the transvoxel algorithm.
    /// </summary>
    public static class TransvoxelCommon
    {
        /// <summary>
        /// Determines whether or not any given position in the voxel data should be
        /// pushed back to make room for transition cells.
        /// </summary>
        /// <param name="pos">Position to check.</param>
        /// <param name="size">Single dimension size of the voxel data.</param>
        /// <param name="chunkFacesWithLowerLod">Bit mask of chunk faces that transition to lower LOD.</param>
        public static bool PositionShouldUseBePushedBack(
            Vector3Int pos,
            int size,
            CubeFaceDirection chunkFacesWithLowerLod)
        {
            var positionFaceMask = ConvertPositionToFaceMask(pos, size);
            if (positionFaceMask == 0 || !FaceTransitionsToLowerLod(positionFaceMask, chunkFacesWithLowerLod))
            {
                return false;
            }

            //Figure 4.13 in the paper shows cases where LOD transitions should not cause
            //edge positions to be pushed back.
            if (FaceMaskIsEdge(positionFaceMask))
            {
                return ((int)chunkFacesWithLowerLod & positionFaceMask) == positionFaceMask;
            }

            return true;
        }

        /// <summary>
        /// Calculates the position for vertices that need to be pushed back to make room for transition cells.
        /// </summary>
        /// <param name="primaryPosition">Primary position of the vertex to push back.</param>
        /// <param name="normal">The normal of the vertex.</param>
        /// <param name="size">The single dimensional size of the voxel data.</param>
        public static Vector3 GetSecondaryVertexPosition(Vector3 primaryPosition, Vector3 normal, int size)
        {
            var delta = GetBorderOffset(primaryPosition, size);
            delta = ProjectBorderOffset(delta, normal);
            return primaryPosition + delta;
        }

        private static int ConvertPositionToFaceMask(Vector3Int pos, int size)
        {
            var faceMask = 0;
            faceMask |= (pos.x == 0 ? 1 : 0) << 0;
            faceMask |= (pos.x == size ? 1 : 0) << 1;
            faceMask |= (pos.y == 0 ? 1 : 0) << 2;
            faceMask |= (pos.y == size ? 1 : 0) << 3;
            faceMask |= (pos.z == 0 ? 1 : 0) << 4;
            faceMask |= (pos.z == size ? 1 : 0) << 5;
            return faceMask;
        }

        private static bool FaceTransitionsToLowerLod(int faceMask, CubeFaceDirection chunkFacesWithLowerLod)
        {
            var transitionMask = (int)chunkFacesWithLowerLod;
            if (transitionMask == 0)
            {
                return false;
            }

            return (transitionMask & faceMask) > 0;
        }

        private static bool FaceMaskIsEdge(int positionFaceMask)
        {
            //Checks if the mask is not a power of 2. This tells us whether
            //or not 2 or more bits in the mask are set, which indicates that
            //the coordinates used to create the mask or on the edge of
            //the terrain chunk cube.
            return (positionFaceMask & (positionFaceMask - 1)) != 0;
        }

        /// <summary>
        /// Implementation of equation 4.2 in the Transvoxel paper.
        /// </summary>
        private static Vector3 GetBorderOffset(Vector3 pos, int size)
        {
            var delta = new Vector3();

            //IDK why, but I only ever need to calculate offset with lod index 0.
            const int lodIndex = 0;
            var p2k = 1 << lodIndex; //2 ^ lod
            var p2mk = 1f / p2k; //2 ^ (-lod)

            const float transitionCellScale = 0.25f;
            var wk = transitionCellScale * p2k;

            for (var i = 0; i < 3; i++)
            {
                var p = pos[i];
                var s = size - 1;

                if (p < p2k)
                {
                    delta[i] = (1f - p2mk * p) * wk;
                }
                else if (p > (p2k * (s - 1)))
                {
                    delta[i] = (s - 1 - p2mk * p) * wk;
                }
                else
                {
                    delta[i] = 0;
                }
            }

            return delta;
        }

        /// <summary>
        /// Implementation of equation 4.3 in the Transvoxel paper.
        /// </summary>
        private static Vector3 ProjectBorderOffset(Vector3 delta, Vector3 normal)
        {
            var defaultProjection = new Vector3(
                (1 - normal.x * normal.x) * delta.x - normal.y * normal.x * delta.y - normal.z * normal.x * delta.z,
                -normal.x * normal.y * delta.x + (1 - normal.y * normal.y) * delta.y - normal.z * normal.y * delta.z,
                -normal.x * normal.z * delta.x - normal.y * normal.z * delta.y + (1 - normal.z * normal.z) * delta.z);

            var newProjection = new Vector3();
            newProjection.x = defaultProjection.z;
            newProjection.y = defaultProjection.y;
            newProjection.z = defaultProjection.x;

            return defaultProjection;
        }
    }
}