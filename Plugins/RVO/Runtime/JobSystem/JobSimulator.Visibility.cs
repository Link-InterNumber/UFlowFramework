using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace RVO.JobSystem
{
    public sealed partial class JobSimulator
    {
        private struct ObstacleSegment
        {
            public float3 start;
            public float3 end;
            public float3 boundsMin;
            public float3 boundsMax;
            public float3 center;
        }

        private void EnsureObstacleVisibilityTree()
        {
            if (!_obstacleTreeDirty)
            {
                return;
            }

            if (_obstacles.Count == 0)
            {
                _obstacleVisibilityTree = null;
                _obstacleVisibilitySegments?.Clear();
                _obstacleTreeDirty = false;
                return;
            }

            if (_obstacleVisibilitySegments == null)
            {
                _obstacleVisibilitySegments = new List<ObstacleSegment>(_obstacles.Count);
            }
            else
            {
                _obstacleVisibilitySegments.Clear();
            }

            for (var index = 0; index < _obstacles.Count; index++)
            {
                var obstacle = _obstacles[index];
                if ((uint)obstacle.next >= (uint)_obstacles.Count)
                {
                    continue;
                }

                var start = ToFloat3(obstacle.point);
                var end = ToFloat3(_obstacles[obstacle.next].point);
                _obstacleVisibilitySegments.Add(new ObstacleSegment
                {
                    start = start,
                    end = end,
                    boundsMin = math.min(start, end),
                    boundsMax = math.max(start, end),
                    center = (start + end) * 0.5f,
                });
            }

            if (_obstacleVisibilitySegments.Count == 0)
            {
                _obstacleVisibilityTree = null;
                _obstacleTreeDirty = false;
                return;
            }

            Span<int> indices = stackalloc int[_obstacleVisibilitySegments.Count];
            for (var i = 0; i < _obstacleVisibilitySegments.Count; i++)
            {
                indices[i] = i;
            }

            _obstacleVisibilityTree = BuildObstacleVisibilityTree(indices);
            _obstacleTreeDirty = false;
        }

        private ObstacleVisibilityTreeNode BuildObstacleVisibilityTree(Span<int> indices)
        {
            if (indices == null || indices.Length == 0)
            {
                return null;
            }

            var node = new ObstacleVisibilityTreeNode
            {
                boundsMin = new float3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
                boundsMax = new float3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity),
            };

            for (var i = 0; i < indices.Length; i++)
            {
                var segment = _obstacleVisibilitySegments[indices[i]];
                node.boundsMin = math.min(node.boundsMin, segment.boundsMin);
                node.boundsMax = math.max(node.boundsMax, segment.boundsMax);
            }

            if (indices.Length <= 8)
            {
                node.segmentIndices = indices.ToArray();
                return node;
            }

            var extents = node.boundsMax - node.boundsMin;
            var axis = extents.x >= extents.y
                ? (extents.x >= extents.z ? 0 : 2)
                : (extents.y >= extents.z ? 1 : 2);
            
            MathUtil.QuickSort<int>(indices, (lhs, rhs) =>
            {
                var lhsCenter = _obstacleVisibilitySegments[lhs].center;
                var rhsCenter = _obstacleVisibilitySegments[rhs].center;
                switch (axis)
                {
                    case 0:
                        return lhsCenter.x.CompareTo(rhsCenter.x);
                    case 1:
                        return lhsCenter.y.CompareTo(rhsCenter.y);
                    default:
                        return lhsCenter.z.CompareTo(rhsCenter.z);
                }
            });
            
            var split = indices.Length / 2;
            if (split <= 0 || split >= indices.Length)
            {
                node.segmentIndices = indices.ToArray();
                return node;
            }

            var leftIndices = indices.Slice(0, split);
            var rightIndices = indices.Slice(split, indices.Length - split);

            node.left = BuildObstacleVisibilityTree(leftIndices);
            node.right = BuildObstacleVisibilityTree(rightIndices);
            return node;
        }

        private bool QueryVisibilityTree(float3 q1, float3 q2, float radiusSq, ObstacleVisibilityTreeNode node)
        {
            if (node == null)
            {
                return true;
            }

            if (!CapsuleBoundsOverlap(q1, q2, radiusSq, node.boundsMin, node.boundsMax))
            {
                return true;
            }

            if (node.segmentIndices != null)
            {
                for (var i = 0; i < node.segmentIndices.Length; i++)
                {
                    var segment = _obstacleVisibilitySegments[node.segmentIndices[i]];
                    if (MathUtil.DistSqSegmentSegment(q1, q2, segment.start, segment.end) <= radiusSq)
                    {
                        return false;
                    }
                }

                return true;
            }

            if (!QueryVisibilityTree(q1, q2, radiusSq, node.left))
            {
                return false;
            }

            return QueryVisibilityTree(q1, q2, radiusSq, node.right);
        }

        private static bool CapsuleBoundsOverlap(float3 q1, float3 q2, float radiusSq, float3 boundsMin, float3 boundsMax)
        {
            var radius = math.sqrt(math.max(radiusSq, 0.0f));
            var radius3 = new float3(radius, radius, radius);
            var capsuleMin = math.min(q1, q2) - radius3;
            var capsuleMax = math.max(q1, q2) + radius3;

            return capsuleMin.x <= boundsMax.x &&
                   capsuleMax.x >= boundsMin.x &&
                   capsuleMin.y <= boundsMax.y &&
                   capsuleMax.y >= boundsMin.y &&
                   capsuleMin.z <= boundsMax.z &&
                   capsuleMax.z >= boundsMin.z;
        }
    }
}
