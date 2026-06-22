using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace PowerCellStudio
{
    public partial class FastAStar: IDisposable
    {
        private NativeArray<bool> _groundTiles;
        private NativeArray<int2> _squareDirections;
        private NativeArray<int2> _hexEvenRowDirections;
        private NativeArray<int2> _hexOddRowDirections;

        private int2 _mapMin;
        private int2 _mapMax;
        private int2 _cardSize;

        private bool _isHex;

        public FastAStar(IEnumerable<Vector2Int> tiles, Vector2Int cardSizeV, bool isHex)
        {
            SetGround(tiles, cardSizeV, isHex);
            _squareDirections = new NativeArray<int2>(SquareDirections, Allocator.Persistent);
            _hexEvenRowDirections = new NativeArray<int2>(HexEvenRowDirections, Allocator.Persistent);
            _hexOddRowDirections = new NativeArray<int2>(HexOddRowDirections, Allocator.Persistent);
        }
        
        public void SetGround(IEnumerable<Vector2Int> tiles, Vector2Int cardSizeV, bool isHex)
        {
            _cardSize = new int2(
                math.max(1, cardSizeV.x),
                math.max(1, cardSizeV.y)
            );
            _isHex = isHex;
            if (_groundTiles.IsCreated)
            {
                _groundTiles.Dispose();
            }
            _mapMin = new int2(int.MaxValue, int.MaxValue);
            _mapMax = new int2(int.MinValue, int.MinValue);
            foreach (var vector2Int in tiles)
            {
                if (vector2Int.x < _mapMin.x) _mapMin.x = vector2Int.x;
                if (vector2Int.y < _mapMin.y) _mapMin.y = vector2Int.y;
                if (vector2Int.x > _mapMax.x) _mapMax.x = vector2Int.x;
                if (vector2Int.y > _mapMax.y) _mapMax.y = vector2Int.y;
            }
            var mapWidth = _mapMax.x - _mapMin.x + 1;
            var mapHeight = _mapMax.y - _mapMin.y + 1;
            _groundTiles = new NativeArray<bool>(mapWidth * mapHeight, Allocator.Persistent);
            foreach (var vector2Int in tiles)
            {
                var mapIndex = (vector2Int.y - _mapMin.y) * mapWidth + (vector2Int.x - _mapMin.x);
                _groundTiles[mapIndex] = true;
            }
        }

        public Vector2Int GetNearestGround(Vector2Int from)
        {
            var job = new FastAStarFindNearestJob()
            {
                map = _groundTiles,
                isHex = _isHex,
                cardSize = _cardSize,
                start = new int2(from.x, from.y),
                mapMin = _mapMin,
                mapMax = _mapMax,
                result = new NativeArray<int2>(1, Allocator.TempJob),
                squareDirections = new NativeArray<int2>(SquareDirections, Allocator.TempJob),
                hexEvenRowDirections = new NativeArray<int2>(HexEvenRowDirections, Allocator.TempJob),
                hexOddRowDirections = new NativeArray<int2>(HexOddRowDirections, Allocator.TempJob)
            };
            job.Schedule().Complete();
            var result = job.result[0];
            job.result.Dispose();
            job.squareDirections.Dispose();
            job.hexEvenRowDirections.Dispose();
            job.hexOddRowDirections.Dispose();
            return new Vector2Int(result.x, result.y);
        }

        public static Vector2Int[] Path(IEnumerable<Vector2Int> grounds, Vector2Int from, Vector2Int to, bool isHex)
        {
            using var astar = new FastAStar(grounds, new Vector2Int(1, 1), isHex);
            return astar.Path(from, to);
        }

        public static FastAStarJobContext PathAsync(IEnumerable<Vector2Int> grounds, Vector2Int from, Vector2Int to, bool isHex)
        {
            using var astar = new FastAStar(grounds, new Vector2Int(1, 1), isHex);
            return astar.PathAsync(from, to);
        }

        public Vector2Int[] Path(Vector2Int from, Vector2Int to)
        {
            if (from == to)
            {
                return new Vector2Int[] { from };
            }

            var context = CreateJobContext(from, to, Allocator.TempJob);
            try
            {
                context.PathJobHandle.Complete();
                return BuildPathResult(context.Nodes, context.NodeCount);
            }
            finally
            {
                context.Dispose();
            }
        }

        public FastAStarJobContext PathAsync(Vector2Int from, Vector2Int to)
        {
            if (from == to)
            {
                return default;
            }

            var context = CreateJobContext(from, to, Allocator.Persistent, false);
            // _runningJobs.Add((context, onCompleted));
            // if (_runningJobs.Count == 1)
            // {
            //     ApplicationManager.instance.StartCoroutine(CheckPathJobCompletion());
            // }
            return context;
        }

        // private List<(FastAStarJobContext, Action<Vector2Int[]>)> _runningJobs = new List<(FastAStarJobContext, Action<Vector2Int[]>)>();
        // private IEnumerator CheckPathJobCompletion()
        // {
        //     while (_runningJobs.Count > 0)
        //     {
        //         for (var i = 0; i < _runningJobs.Count;)
        //         {
        //             var (context, onCompleted) = _runningJobs[i];
        //             if (context.IsCompleted)
        //             {
        //                 onCompleted?.Invoke(BuildPathResult(context.Nodes, context.NodeCount));
        //                 context.Dispose();
        //                 _runningJobs.RemoveAt(i);
        //             }
        //             else
        //             {
        //                 i++;
        //             }
        //         }
        //         yield return null;
        //     }
        // }

        private FastAStarJobContext CreateJobContext(Vector2Int from, Vector2Int to, Allocator allocator, bool scheduleImmediately = true)
        {
            var nodes = new NativeList<int2>(allocator);
            var nodeCount = new NativeArray<int>(1, allocator);
            var toNearestResult = new NativeArray<int2>(1, allocator);
            var openList = new NativeList<FastAStarNode>(allocator);
            var closeList = new NativeArray<FastAStarNode>(_groundTiles.Length, allocator);

            var toIndex = (to.y - _mapMin.y) * (_mapMax.x - _mapMin.x + 1) + (to.x - _mapMin.x);
            if (toIndex >= 0 && toIndex < _groundTiles.Length && _groundTiles[toIndex])
            {
                toNearestResult[0] = new int2(to.x, to.y);
                var job = new FastAStarFindPathJob()
                {
                    map = _groundTiles,
                    nodes = nodes,
                    isHex = _isHex,
                    cardSize = _cardSize,
                    nodeCount = nodeCount,
                    start = new int2(from.x, from.y),
                    end = toNearestResult,
                    mapMin = _mapMin,
                    mapMax = _mapMax,
                    squareDirections = _squareDirections,
                    hexEvenRowDirections = _hexEvenRowDirections,
                    hexOddRowDirections = _hexOddRowDirections,
                    openList = openList,
                    closeList = closeList
                };
                return new FastAStarJobContext(
                    nodes,
                    nodeCount,
                    toNearestResult,
                    openList,
                    closeList,
                    job,
                    scheduleImmediately ? job.Schedule() : default
                );
            }
            else
            {
                var findNearestJob = new FastAStarFindNearestJob()
                {
                    map = _groundTiles,
                    isHex = _isHex,
                    cardSize = _cardSize,
                    start = new int2(to.x, to.y),
                    mapMin = _mapMin,
                    mapMax = _mapMax,
                    result = toNearestResult,
                    squareDirections = _squareDirections,
                    hexEvenRowDirections = _hexEvenRowDirections,
                    hexOddRowDirections = _hexOddRowDirections
                };
                var job = new FastAStarFindPathJob()
                {
                    map = _groundTiles,
                    nodes = nodes,
                    isHex = _isHex,
                    cardSize = _cardSize,
                    nodeCount = nodeCount,
                    start = new int2(from.x, from.y),
                    end = toNearestResult,
                    mapMin = _mapMin,
                    mapMax = _mapMax,
                    squareDirections = _squareDirections,
                    hexEvenRowDirections = _hexEvenRowDirections,
                    hexOddRowDirections = _hexOddRowDirections,
                    openList = openList,
                    closeList = closeList
                };
                return new FastAStarJobContext(
                    nodes,
                    nodeCount,
                    toNearestResult,
                    openList,
                    closeList,
                    job,
                    scheduleImmediately ? job.Schedule(findNearestJob.Schedule()) : default
                );
            }
        }

        private static Vector2Int[] BuildPathResult(NativeList<int2> nodes, NativeArray<int> nodeCount)
        {
            if (nodeCount[0] < 0)
            {
                return null;
            }

            var result = new Vector2Int[nodeCount[0]];
            var lastIndex = nodeCount[0] - 1;
            for (var i = lastIndex; i >= 0; i--)
            {
                result[i] = new Vector2Int(nodes[i].x, nodes[i].y);
            }

            return result;
        }

        public void Dispose()
        {
            if (_groundTiles.IsCreated)
            {
                _groundTiles.Dispose();
            }
            if (_squareDirections.IsCreated)
            {
                _squareDirections.Dispose();
            }
            if (_hexEvenRowDirections.IsCreated)
            {
                _hexEvenRowDirections.Dispose();
            }
            if (_hexOddRowDirections.IsCreated)
            {
                _hexOddRowDirections.Dispose();
            }
        }

        public struct FastAStarJobContext : IDisposable
        {
            public NativeList<int2> Nodes;
            public NativeArray<int> NodeCount;
            public NativeArray<int2> ToNearestResult;
            public NativeList<FastAStarNode> OpenList;
            public NativeArray<FastAStarNode> CloseList;
            public FastAStarFindPathJob Job;
            public JobHandle PathJobHandle;

            public FastAStarJobContext(
                NativeList<int2> nodes,
                NativeArray<int> nodeCount,
                NativeArray<int2> toNearestResult,
                NativeList<FastAStarNode> openList,
                NativeArray<FastAStarNode> closeList,
                FastAStarFindPathJob job,
                JobHandle pathJobHandle)
            {
                Nodes = nodes;
                NodeCount = nodeCount;
                ToNearestResult = toNearestResult;
                OpenList = openList;
                CloseList = closeList;
                Job = job;
                PathJobHandle = pathJobHandle;
            }

            public bool IsCompleted => PathJobHandle.IsCompleted;

            public void WaitForCompletion(out Vector2Int[] result)
            {
                result = null;
                if (!IsCompleted)
                {
                    return;
                }
                PathJobHandle.Complete();
                result = new Vector2Int[Nodes.Length];
                for (var i = 0; i < Nodes.Length; i++)
                {
                    result[i] = new Vector2Int(Nodes[i].x, Nodes[i].y);
                }
                Dispose();
            }

            public void Dispose()
            {
                if (Nodes.IsCreated) Nodes.Dispose();
                if (NodeCount.IsCreated) NodeCount.Dispose();
                if (ToNearestResult.IsCreated) ToNearestResult.Dispose();
                if (OpenList.IsCreated) OpenList.Dispose();
                if (CloseList.IsCreated) CloseList.Dispose();
            }
        }
    }
}