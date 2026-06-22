using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace PowerCellStudio
{
    public class FastJPS : IDisposable
    {
        private NativeArray<byte> _groundTiles;
        private int2 _mapMin;
        private int2 _mapMax;
        private int _mapWidth;
        private int _mapHeight;

        public FastJPS(IEnumerable<Vector2Int> tiles)
        {
            SetGround(tiles);
        }

        public void SetGround(IEnumerable<Vector2Int> tiles)
        {
            if (_groundTiles.IsCreated)
            {
                _groundTiles.Dispose();
            }

            var groundSet = new HashSet<int2>();
            _mapMin = new int2(int.MaxValue, int.MaxValue);
            _mapMax = new int2(int.MinValue, int.MinValue);

            foreach (var tile in tiles)
            {
                var pos = new int2(tile.x, tile.y);
                groundSet.Add(pos);
                _mapMin.x = math.min(_mapMin.x, tile.x);
                _mapMin.y = math.min(_mapMin.y, tile.y);
                _mapMax.x = math.max(_mapMax.x, tile.x);
                _mapMax.y = math.max(_mapMax.y, tile.y);
            }

            if (groundSet.Count == 0)
            {
                _groundTiles = default;
                _mapWidth = 0;
                _mapHeight = 0;
                return;
            }

            _mapWidth = _mapMax.x - _mapMin.x + 1;
            _mapHeight = _mapMax.y - _mapMin.y + 1;
            _groundTiles = new NativeArray<byte>(_mapWidth * _mapHeight, Allocator.Persistent);
            foreach (var pos in groundSet)
            {
                _groundTiles[GetMapIndexUnchecked(pos)] = 1;
            }
        }

        public static Vector2Int[] GetPath(List<Vector2Int> grounds, Vector2Int start, Vector2Int destination)
        {
            using var jps = new FastJPS(grounds);
            return jps.GetPath(start, destination);
        }

        public Vector2Int[] GetPath(Vector2Int start, Vector2Int destination)
        {
            if (start == destination)
            {
                return new[] { start };
            }

            if (!_groundTiles.IsCreated || _groundTiles.Length == 0)
            {
                return Array.Empty<Vector2Int>();
            }

            var nodes = new NativeList<FastJpsNode>(Allocator.TempJob);
            var openList = new NativeList<int>(Allocator.TempJob);
            var closeMap = new NativeArray<byte>(_groundTiles.Length, Allocator.TempJob);
            var nodeIndexMap = new NativeArray<int>(_groundTiles.Length, Allocator.TempJob);
            var path = new NativeList<int2>(Allocator.TempJob);
            var pathCount = new NativeArray<int>(1, Allocator.TempJob);

            try
            {
                for (var i = 0; i < nodeIndexMap.Length; i++)
                {
                    nodeIndexMap[i] = -1;
                }

                var job = new FindPathJob()
                {
                    map = _groundTiles,
                    mapMin = _mapMin,
                    mapMax = _mapMax,
                    mapWidth = _mapWidth,
                    start = new int2(start.x, start.y),
                    destination = new int2(destination.x, destination.y),
                    nodes = nodes,
                    openList = openList,
                    closeMap = closeMap,
                    nodeIndexMap = nodeIndexMap,
                    path = path,
                    pathCount = pathCount
                };

                job.Schedule().Complete();
                if (pathCount[0] <= 0)
                {
                    return Array.Empty<Vector2Int>();
                }

                var result = new Vector2Int[pathCount[0]];
                var lastIndex = pathCount[0] - 1;
                for (var i = 0; i <= lastIndex; i++)
                {
                    var pos = path[lastIndex - i];
                    result[i] = new Vector2Int(pos.x, pos.y);
                }

                return result;
            }
            finally
            {
                nodes.Dispose();
                openList.Dispose();
                closeMap.Dispose();
                nodeIndexMap.Dispose();
                path.Dispose();
                pathCount.Dispose();
            }
        }

        public void Dispose()
        {
            if (_groundTiles.IsCreated)
            {
                _groundTiles.Dispose();
            }
        }

        private int GetMapIndexUnchecked(int2 pos)
        {
            return (pos.y - _mapMin.y) * _mapWidth + (pos.x - _mapMin.x);
        }

        public struct Point
        {
            public int x;
            public int y;
            public float F;
            public float G;
            public float H;

            public Point(int x, int y)
            {
                this.x = x;
                this.y = y;
                F = 0f;
                G = 0f;
                H = 0f;
            }

            public Point(int2 pos)
            {
                x = pos.x;
                y = pos.y;
                F = 0f;
                G = 0f;
                H = 0f;
            }

            public int2 Vector2Int => new int2(x, y);
        }

        private struct FastJpsNode
        {
            public int2 pos;
            public float G;
            public float H;
            public float F;
            public int parentIndex;
        }

        [BurstCompile]
        private struct FindPathJob : IJob
        {
            [ReadOnly]
            public NativeArray<byte> map;

            public int2 mapMin;
            public int2 mapMax;
            public int mapWidth;
            public int2 start;
            public int2 destination;

            public NativeList<FastJpsNode> nodes;
            public NativeList<int> openList;
            public NativeArray<byte> closeMap;
            public NativeArray<int> nodeIndexMap;
            public NativeList<int2> path;
            public NativeArray<int> pathCount;

            public void Execute()
            {
                pathCount[0] = 0;
                if (!IsWalkable(start) || !IsWalkable(destination))
                {
                    return;
                }

                var startNode = new FastJpsNode()
                {
                    pos = start,
                    G = 0f,
                    H = CalculateH(start),
                    F = CalculateH(start),
                    parentIndex = -1
                };

                AddOrUpdateOpenNode(startNode);
                while (openList.Length > 0)
                {
                    var currentIndex = PopLeastFNodeIndex();
                    var currentNode = nodes[currentIndex];
                    var currentMapIndex = GetMapIndex(currentNode.pos);
                    if (currentMapIndex < 0)
                    {
                        continue;
                    }

                    if (closeMap[currentMapIndex] == 1)
                    {
                        continue;
                    }

                    closeMap[currentMapIndex] = 1;
                    if (currentNode.pos.Equals(destination))
                    {
                        BuildPath(currentIndex);
                        return;
                    }

                    StraightSearch(currentIndex);
                    DiagonalSearch(currentIndex);
                }
            }

            private void StraightSearch(int currentIndex)
            {
                TryAddJumpPoint(LineSearch(nodes[currentIndex].pos, new int2(1, 0)), currentIndex);
                TryAddJumpPoint(LineSearch(nodes[currentIndex].pos, new int2(0, 1)), currentIndex);
                TryAddJumpPoint(LineSearch(nodes[currentIndex].pos, new int2(-1, 0)), currentIndex);
                TryAddJumpPoint(LineSearch(nodes[currentIndex].pos, new int2(0, -1)), currentIndex);
            }

            private void DiagonalSearch(int currentIndex)
            {
                TryAddJumpPoint(DiagonalLineSearch(nodes[currentIndex].pos, new int2(1, 1)), currentIndex);
                TryAddJumpPoint(DiagonalLineSearch(nodes[currentIndex].pos, new int2(1, -1)), currentIndex);
                TryAddJumpPoint(DiagonalLineSearch(nodes[currentIndex].pos, new int2(-1, 1)), currentIndex);
                TryAddJumpPoint(DiagonalLineSearch(nodes[currentIndex].pos, new int2(-1, -1)), currentIndex);
            }

            private int2 LineSearch(int2 current, int2 dir)
            {
                var temp = current + dir;
                while (true)
                {
                    if (temp.Equals(destination))
                    {
                        return temp;
                    }

                    if (!IsWalkable(temp))
                    {
                        return InvalidPos();
                    }

                    if (dir.x != 0 && dir.y == 0)
                    {
                        if ((!IsWalkable(temp + new int2(0, 1)) && IsWalkable(temp + new int2(dir.x, 1)) && IsWalkable(temp + new int2(dir.x, 0)))
                            || (!IsWalkable(temp + new int2(0, -1)) && IsWalkable(temp + new int2(dir.x, -1)) && IsWalkable(temp + new int2(dir.x, 0))))
                        {
                            return temp;
                        }
                    }
                    else if (dir.y != 0 && dir.x == 0)
                    {
                        if ((!IsWalkable(temp + new int2(1, 0)) && IsWalkable(temp + new int2(1, dir.y)) && IsWalkable(temp + new int2(0, dir.y)))
                            || (!IsWalkable(temp + new int2(-1, 0)) && IsWalkable(temp + new int2(-1, dir.y)) && IsWalkable(temp + new int2(0, dir.y))))
                        {
                            return temp;
                        }
                    }

                    temp += dir;
                }
            }

            private int2 DiagonalLineSearch(int2 current, int2 dir)
            {
                var temp = current + dir;
                while (true)
                {
                    if (!IsWalkable(temp))
                    {
                        return InvalidPos();
                    }

                    if (temp.Equals(destination))
                    {
                        return temp;
                    }

                    if (HasDiagonalForcedNeighbour(temp, dir))
                    {
                        return temp;
                    }

                    var horizontalJump = LineSearch(temp, new int2(dir.x, 0));
                    var verticalJump = LineSearch(temp, new int2(0, dir.y));
                    if (IsValidPos(horizontalJump) || IsValidPos(verticalJump))
                    {
                        return temp;
                    }

                    temp += dir;
                }
            }

            private bool HasDiagonalForcedNeighbour(int2 temp, int2 dir)
            {
                if (dir.x > 0 && dir.y > 0)
                {
                    return (!IsWalkable(temp + new int2(0, -dir.y)) && IsWalkable(temp + new int2(1, -dir.y)) && IsWalkable(temp + new int2(1, 0)))
                           || (!IsWalkable(temp + new int2(-dir.x, 0)) && IsWalkable(temp + new int2(-dir.x, 1)) && IsWalkable(temp + new int2(0, 1)));
                }

                if (dir.x > 0 && dir.y < 0)
                {
                    return (!IsWalkable(temp + new int2(0, -dir.y)) && IsWalkable(temp + new int2(1, -dir.y)) && IsWalkable(temp + new int2(1, 0)))
                           || (!IsWalkable(temp + new int2(-dir.x, 0)) && IsWalkable(temp + new int2(-dir.x, -1)) && IsWalkable(temp + new int2(0, -1)));
                }

                if (dir.x < 0 && dir.y > 0)
                {
                    return (!IsWalkable(temp + new int2(-dir.x, 0)) && IsWalkable(temp + new int2(-dir.x, 1)) && IsWalkable(temp + new int2(0, 1)))
                           || (!IsWalkable(temp + new int2(0, -dir.y)) && IsWalkable(temp + new int2(-1, -dir.y)) && IsWalkable(temp + new int2(-1, 0)));
                }

                return (!IsWalkable(temp + new int2(-dir.x, 0)) && IsWalkable(temp + new int2(-dir.x, -1)) && IsWalkable(temp + new int2(0, -1)))
                       || (!IsWalkable(temp + new int2(0, -dir.y)) && IsWalkable(temp + new int2(-1, -dir.y)) && IsWalkable(temp + new int2(-1, 0)));
            }

            private void TryAddJumpPoint(int2 jumpPoint, int parentIndex)
            {
                if (!IsValidPos(jumpPoint))
                {
                    return;
                }

                var mapIndex = GetMapIndex(jumpPoint);
                if (mapIndex < 0 || closeMap[mapIndex] == 1)
                {
                    return;
                }

                var parentNode = nodes[parentIndex];
                var g = parentNode.G + CalculateMoveCost(parentNode.pos, jumpPoint);
                var h = CalculateH(jumpPoint);
                var node = new FastJpsNode()
                {
                    pos = jumpPoint,
                    G = g,
                    H = h,
                    F = g + h,
                    parentIndex = parentIndex
                };

                AddOrUpdateOpenNode(node);
            }

            private void AddOrUpdateOpenNode(FastJpsNode node)
            {
                var mapIndex = GetMapIndex(node.pos);
                if (mapIndex < 0)
                {
                    return;
                }

                var nodeIndex = nodeIndexMap[mapIndex];
                if (nodeIndex < 0)
                {
                    nodeIndex = nodes.Length;
                    nodes.Add(node);
                    nodeIndexMap[mapIndex] = nodeIndex;
                    PushOpenIndex(nodeIndex);
                    return;
                }

                if (node.F < nodes[nodeIndex].F || (math.abs(node.F - nodes[nodeIndex].F) < 0.0001f && node.G < nodes[nodeIndex].G))
                {
                    nodes[nodeIndex] = node;
                    var heapIndex = FindOpenHeapIndex(nodeIndex);
                    if (heapIndex >= 0)
                    {
                        SiftUp(heapIndex);
                        SiftDown(heapIndex);
                    }
                    else
                    {
                        PushOpenIndex(nodeIndex);
                    }
                }
            }

            private int PopLeastFNodeIndex()
            {
                var result = openList[0];
                var lastIndex = openList.Length - 1;
                if (lastIndex == 0)
                {
                    openList.RemoveAtSwapBack(0);
                    return result;
                }

                openList[0] = openList[lastIndex];
                openList.RemoveAtSwapBack(lastIndex);
                SiftDown(0);
                return result;
            }

            private void PushOpenIndex(int nodeIndex)
            {
                openList.Add(nodeIndex);
                SiftUp(openList.Length - 1);
            }

            private void SiftUp(int index)
            {
                while (index > 0)
                {
                    var parentIndex = (index - 1) >> 1;
                    if (!IsBetter(openList[index], openList[parentIndex]))
                    {
                        break;
                    }

                    SwapOpen(index, parentIndex);
                    index = parentIndex;
                }
            }

            private void SiftDown(int index)
            {
                while (true)
                {
                    var leftIndex = index * 2 + 1;
                    if (leftIndex >= openList.Length)
                    {
                        break;
                    }

                    var rightIndex = leftIndex + 1;
                    var bestIndex = leftIndex;
                    if (rightIndex < openList.Length && IsBetter(openList[rightIndex], openList[leftIndex]))
                    {
                        bestIndex = rightIndex;
                    }

                    if (!IsBetter(openList[bestIndex], openList[index]))
                    {
                        break;
                    }

                    SwapOpen(index, bestIndex);
                    index = bestIndex;
                }
            }

            private bool IsBetter(int nodeIndexA, int nodeIndexB)
            {
                var nodeA = nodes[nodeIndexA];
                var nodeB = nodes[nodeIndexB];
                if (math.abs(nodeA.F - nodeB.F) > 0.0001f)
                {
                    return nodeA.F < nodeB.F;
                }

                return nodeA.H < nodeB.H;
            }

            private void SwapOpen(int indexA, int indexB)
            {
                var temp = openList[indexA];
                openList[indexA] = openList[indexB];
                openList[indexB] = temp;
            }

            private int FindOpenHeapIndex(int nodeIndex)
            {
                for (var i = 0; i < openList.Length; i++)
                {
                    if (openList[i] == nodeIndex)
                    {
                        return i;
                    }
                }

                return -1;
            }

            private void BuildPath(int nodeIndex)
            {
                while (nodeIndex >= 0)
                {
                    var node = nodes[nodeIndex];
                    path.Add(node.pos);
                    nodeIndex = node.parentIndex;
                }

                pathCount[0] = path.Length;
            }

            private float CalculateMoveCost(int2 from, int2 to)
            {
                var delta = math.abs(to - from);
                var diagonal = math.min(delta.x, delta.y);
                var straight = math.max(delta.x, delta.y) - diagonal;
                return diagonal * 1.41421356237f + straight;
            }

            private float CalculateH(int2 pos)
            {
                var delta = math.abs(destination - pos);
                var diagonal = math.min(delta.x, delta.y);
                var straight = math.max(delta.x, delta.y) - diagonal;
                return diagonal * 1.41421356237f + straight;
            }

            private bool IsWalkable(int2 pos)
            {
                var index = GetMapIndex(pos);
                return index >= 0 && map[index] == 1;
            }

            private int GetMapIndex(int2 pos)
            {
                if (pos.x < mapMin.x || pos.x > mapMax.x || pos.y < mapMin.y || pos.y > mapMax.y)
                {
                    return -1;
                }

                return (pos.y - mapMin.y) * mapWidth + (pos.x - mapMin.x);
            }

            private bool IsValidPos(int2 pos)
            {
                return pos.x != int.MinValue && pos.y != int.MinValue;
            }

            private int2 InvalidPos()
            {
                return new int2(int.MinValue, int.MinValue);
            }
        }
    }
}
