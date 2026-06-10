using System;
using System.Collections.Generic;
using UnityEngine;

namespace RVO.JobSystem
{
    public sealed partial class JobSimulator
    {
        private int _obstacleId;
        public int AddObstacle(IList<Vector3> vertices)
        {
            ThrowIfDisposed();

            if (vertices == null || vertices.Count < 2)
            {
                return -1;
            }

            var obstacleNo = _obstacles.Count;

            for (var i = 0; i < vertices.Count; ++i)
            {
                var next = i == vertices.Count - 1 ? 0 : i + 1;
                var previous = i == 0 ? vertices.Count - 1 : i - 1;
                var direction = MathUtil.NormalizeSafe(ToFloat3(vertices[next] - vertices[i]));

                var obstacle = new ManagedObstacleState
                {
                    id = _obstacleId++,
                    point = vertices[i],
                    direction = direction,
                    previous = obstacleNo + previous,
                    next = obstacleNo + next,
                    convex = vertices.Count == 2 || MathUtil.LeftOf(ToFloat3(vertices[previous]), ToFloat3(vertices[i]), ToFloat3(vertices[next])) >= 0.0f,
                };

                _obstacles.Add(obstacle);
            }

            _nativeObstacleDirty = true;
            _obstacleTreeDirty = true;
            _obstacleVisibilityTree = null;
            return _obstacles[obstacleNo].id;
        }

        public void RemoveObstacle(int obstacleNo)
        {
            ThrowIfDisposed();
            
            // 二分查找
            var index = MathUtil.BinarySearch(_obstacles, obstacleNo);
            
            var verticesCount = 0;
            for (var i = index; i < _obstacles.Count; i++)
            {
                verticesCount++;
                if (_obstacles[i].next == index)
                {
                    break;
                }
            }
            _obstacles.RemoveRange(index, verticesCount);

            for (var i = index; i < _obstacles.Count; i++)
            {
                var data = _obstacles[i];
                data.previous -= verticesCount;
                data.next -= verticesCount;
                _obstacles[i] = data;
            }

            _nativeObstacleDirty = true;
            _obstacleTreeDirty = true;
            _obstacleVisibilityTree = null;
        }

        public void ProcessObstacles()
        {
            ThrowIfDisposed();
            EnsureObstacleVisibilityTree();
        }

        public bool QueryVisibility(Vector3 point1, Vector3 point2, float radius)
        {
            ThrowIfDisposed();
            if (_obstacles == null || _obstacles.Count == 0)
            {
                return true;
            }

            var q1 = ToFloat3(point1);
            var q2 = ToFloat3(point2);
            var radiusSq = radius * radius;
            EnsureObstacleVisibilityTree();

            return QueryVisibilityTree(q1, q2, radiusSq, _obstacleVisibilityTree);
        }

        public int GetNumObstacleVertices()
        {
            return _obstacles.Count;
        }

        public Vector3 GetObstacleVertex(int vertexNo)
        {
            ThrowIfDisposed();

            if (vertexNo < 0 || vertexNo >= _obstacles.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(vertexNo));
            }

            return _obstacles[vertexNo].point;
        }

        public int GetNextObstacleVertexNo(int vertexNo)
        {
            ThrowIfDisposed();

            if (vertexNo < 0 || vertexNo >= _obstacles.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(vertexNo));
            }

            return _obstacles[vertexNo].next;
        }

        public int GetPrevObstacleVertexNo(int vertexNo)
        {
            ThrowIfDisposed();

            if (vertexNo < 0 || vertexNo >= _obstacles.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(vertexNo));
            }

            return _obstacles[vertexNo].previous;
        }
    }
}
