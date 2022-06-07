using System.Collections.Generic;
using UnityEngine;

namespace STerrain.EndlessTerrain
{
    /// <summary>
    /// Implementation of the octree algorithm.
    /// https://en.wikipedia.org/wiki/Octree
    /// </summary>
    public class Octree
    {
        private readonly Bounds _bounds;
        private readonly int _divisionCount;

        private Octree[] _children;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="bounds">Represents the cube bounds of this octree node.</param>
        /// <param name="divisionCount">Number of times to divide the octree.</param>
        public Octree(Bounds bounds, int divisionCount)
        {
            _bounds = bounds;
            _divisionCount = divisionCount;
        }

        /// <summary>
        /// Inserts a position in the octree, causing it to subdivide until a minimum size is reached.
        /// </summary>
        /// <param name="position">The insert position.</param>
        public void Insert(Vector3 position)
        {
            if (_divisionCount == 0)
            {
                return;
            }

            CreateChildren(position);
        }

        /// <summary>
        /// Gets a list of all the bounds of every octree leaf node.
        /// </summary>
        public List<Bounds> GetAllLeafBounds()
        {
            var bounds = new List<Bounds> { };
            if (_children == null)
            {
                //This is a leaf node.
                return new List<Bounds> { _bounds };
            }
            else
            {
                for (var i = 0; i < 8; i++)
                {
                    bounds.AddRange(_children[i].GetAllLeafBounds());
                }
            }
            return bounds;
        }

        /// <summary>
        /// Gets the bounds for a leaf node given a specific position.
        /// </summary>
        /// <param name="position">The position to get the bounding box for.</param>
        public Bounds? GetBound(Vector3 position)
        {
            if (_children == null)
            {
                if (_bounds.Contains(position))
                {
                    return _bounds;
                }
                else
                {
                    return null;
                }
            }

            for (var i = 0; i < _children.Length; i++)
            {
                var childBounds = _children[i].GetBound(position);
                if (childBounds != null)
                {
                    return childBounds;
                }
            }

            return null;
        }

        private void CreateChildren(Vector3 position)
        {
            _children = new Octree[8];
            for (var i = 0; i < 8; i++)
            {
                var childDivisionCount = _divisionCount - 1;
                var childBounds = GetChildBoundsFromIndex(i);
                if (!childBounds.Contains(position))
                {
                    childDivisionCount = 0;
                }

                _children[i] = new Octree(GetChildBoundsFromIndex(i), childDivisionCount);
                _children[i].Insert(position);
            }
        }

        private void CreateChildren2(Vector3 position)
        {
            _children = new Octree[8];
            var childBoundsList = new List<Bounds>();
            var minBoundCenterSqrMagnitude = float.MaxValue;
            var minBounds = _bounds;
            for (var i = 0; i < 8; i++)
            {
                var childBounds = GetChildBoundsFromIndex(i);
                childBoundsList.Add(childBounds);
                var boundCenterSqrMagnitude = (childBounds.center - position).sqrMagnitude;
                if (boundCenterSqrMagnitude < minBoundCenterSqrMagnitude)
                {
                    minBoundCenterSqrMagnitude = boundCenterSqrMagnitude;
                    minBounds = childBounds;
                }
            }

            for (var i = 0; i < 8; i++)
            {
                var childDivisionCount = 0;
                var childBounds = childBoundsList[i];
                if (childBounds == minBounds)
                {
                    childDivisionCount = _divisionCount - 1;
                }

                _children[i] = new Octree(GetChildBoundsFromIndex(i), childDivisionCount);
                _children[i].Insert(position);
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
        private Bounds GetChildBoundsFromIndex(int i)
        {
            var childSize = _bounds.size / 2;
            var offset = childSize / 2;

            switch (i)
            {
                case 0:
                    offset *= -1;
                    break;
                case 1:
                    offset.y *= -1;
                    offset.z *= -1;
                    break;
                case 2:
                    offset.x *= -1;
                    offset.y *= -1;
                    break;
                case 3:
                    offset.y *= -1;
                    break;
                case 4:
                    offset.x *= -1;
                    offset.z *= -1;
                    break;
                case 5:
                    offset.z *= -1;
                    break;
                case 6:
                    offset.x *= -1;
                    break;
            }

            return new Bounds(_bounds.center + offset, childSize);
        }

        /// <summary>
        /// Draws wire cubes representing the entire octree.
        /// </summary>
        public void DrawGizmo()
        {
            Gizmos.DrawWireCube(_bounds.center, _bounds.size);
            if (_children != null)
            {
                for (var i = 0; i < _children.Length; i++)
                {
                    _children[i].DrawGizmo();
                }
            }
        }
    }
}