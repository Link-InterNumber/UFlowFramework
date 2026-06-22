using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace PowerCellStudio
{
    public partial class FastAStar
    {
        [BurstCompile]
        public struct FastAStarFindNearestJob : IJob
        {
            // map: 0不可走，1可走，2已走过
            public NativeArray<bool> map;
            public bool isHex;
            public int2 cardSize;
            public int2 start;
            public int2 mapMin;
            public int2 mapMax;
            public NativeArray<int2> result;

            public NativeArray<int2> squareDirections;
            public NativeArray<int2> hexEvenRowDirections;
            public NativeArray<int2> hexOddRowDirections;
             
            public void Execute()
            {
                for (var i = 0; i < result.Length; i++)
                {
                    result[i] = new int2(-1, -1);
                }
                if (IsValidMovePosition(start))
                {
                    result[0] = start;
                    return;
                }

                var maxRadius = GetMaxSearchRadius();
                for (var radius = 1; radius <= maxRadius; radius++)
                {
                    var hasResult = false;
                    var bestPos = int2.zero;
                    var bestScore = int.MaxValue;

                    ScanHorizontalEdge(start.y - radius, start.x - radius, start.x + radius, ref hasResult, ref bestPos, ref bestScore);
                    ScanHorizontalEdge(start.y + radius, start.x - radius, start.x + radius, ref hasResult, ref bestPos, ref bestScore);
                    ScanVerticalEdge(start.x - radius, start.y - radius + 1, start.y + radius - 1, ref hasResult, ref bestPos, ref bestScore);
                    ScanVerticalEdge(start.x + radius, start.y - radius + 1, start.y + radius - 1, ref hasResult, ref bestPos, ref bestScore);

                    if (hasResult)
                    {
                        result[0] = bestPos;
                        return;
                    }
                }
            }

            private void ScanHorizontalEdge(int y, int minX, int maxX, ref bool hasResult, ref int2 bestPos, ref int bestScore)
            {
                if (y < mapMin.y || y > mapMax.y)
                {
                    return;
                }

                var clampedMinX = math.max(minX, mapMin.x);
                var clampedMaxX = math.min(maxX, mapMax.x);
                for (var x = clampedMinX; x <= clampedMaxX; x++)
                {
                    TrySetBest(new int2(x, y), ref hasResult, ref bestPos, ref bestScore);
                }
            }

            private void ScanVerticalEdge(int x, int minY, int maxY, ref bool hasResult, ref int2 bestPos, ref int bestScore)
            {
                if (x < mapMin.x || x > mapMax.x || minY > maxY)
                {
                    return;
                }

                var clampedMinY = math.max(minY, mapMin.y);
                var clampedMaxY = math.min(maxY, mapMax.y);
                for (var y = clampedMinY; y <= clampedMaxY; y++)
                {
                    TrySetBest(new int2(x, y), ref hasResult, ref bestPos, ref bestScore);
                }
            }

            private void TrySetBest(int2 pos, ref bool hasResult, ref int2 bestPos, ref int bestScore)
            {
                if (!IsValidMovePosition(pos))
                {
                    return;
                }

                var score = GetTieBreakScore(pos);
                if (!hasResult || score < bestScore)
                {
                    hasResult = true;
                    bestPos = pos;
                    bestScore = score;
                }
            }

            private int GetTieBreakScore(int2 pos)
            {
                var delta = math.abs(pos - start);
                return delta.x * delta.x + delta.y * delta.y;
            }

            private int GetMaxSearchRadius()
            {
                var left = math.abs(start.x - mapMin.x);
                var right = math.abs(start.x - mapMax.x);
                var down = math.abs(start.y - mapMin.y);
                var up = math.abs(start.y - mapMax.y);
                return math.max(math.max(left, right), math.max(down, up));
            }

            private bool IsValidMovePosition(int2 pos)
            {
                var index = GetMapIndex(pos);
                return index >= 0 && index < map.Length && map[index] && IsPosSizeFit(pos);
            }
            
            private int GetMapIndex(int2 pos)
            {
                if (pos.x < mapMin.x || pos.x > mapMax.x || pos.y < mapMin.y || pos.y > mapMax.y)
                {
                    return -1;
                }

                var index = (pos.y - mapMin.y) * (mapMax.x - mapMin.x + 1) + (pos.x - mapMin.x);
                return index;
            }

            private int2 MapIndexToPos(int index)
            {
                var x = index % (mapMax.x - mapMin.x + 1) + mapMin.x;
                var y = index / (mapMax.x - mapMin.x + 1) + mapMin.y;
                return new int2(x, y);
            }

            private bool IsPosSizeFit(int2 pos)
            {
                if (cardSize.x == 1 && cardSize.y == 1) return true;
                for (var x = 0; x < cardSize.x; x++)
                {
                    for (var y = 0; y < cardSize.y; y++)
                    {
                        var checkPos = pos + new int2(x, y);
                        if (checkPos.x < mapMin.x || checkPos.x > mapMax.x || checkPos.y < mapMin.y ||
                            checkPos.y > mapMax.y)
                        {
                            return false;
                        }

                        var newMapIndex = GetMapIndex(checkPos);
                        if (newMapIndex == -1 || !map[newMapIndex])
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }
    }
}