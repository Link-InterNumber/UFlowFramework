using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace PowerCellStudio
{
    public partial class FastAStar
    {
        [BurstCompile]
        public struct FastAStarFindPathJob : IJob
        {
            // 融合closeList
            // map: 0不可走，1可走，2已走过
            public NativeArray<bool> map;
            public NativeList<int2> nodes;
            public bool isHex;
            public int2 cardSize;
            public NativeArray<int> nodeCount;
            public int2 start;
            public NativeArray<int2> end;
            public int2 mapMin;
            public int2 mapMax;

            public NativeArray<int2> squareDirections;
            public NativeArray<int2> hexEvenRowDirections;
            public NativeArray<int2> hexOddRowDirections;
            public NativeList<FastAStarNode> openList;
            public NativeArray<FastAStarNode> closeList;

            public void Execute()
            {
                nodeCount[0] = -1;
                if (start.Equals(end[0]))
                {
                    nodes.Add(start);
                    nodeCount[0] = 1;
                    return;
                }
                if (!IsPosWalkable(start))
                {
                    return;
                }

                var startNode = new FastAStarNode()
                {
                    pos = start,
                    G = 0,
                    H = math.abs(end[0].x - start.x) + math.abs(end[0].y - start.y),
                    I = 0,
                    parentIndex = -1
                };
                PushOpenNode(startNode);

                while (openList.Length > 0)
                {
                    var nearestNode = PopNearestNode();
                    // if (nearestNode.G > map.Length)
                    // {
                    //     break;
                    // }

                    var parentIndex = AddToCloseList(nearestNode);

                    var dir = isHex
                        ? (nearestNode.pos.y % 2 == 0 ? hexEvenRowDirections : hexOddRowDirections)
                        : squareDirections;
                    var lastDir = nearestNode.parentIndex >= 0
                        ? nearestNode.pos - closeList[nearestNode.parentIndex].pos
                        : new int2(0, 0);
                    var lastI = nearestNode.I;
                    for (var i = 0; i < dir.Length; i++)
                    {
                        var newPos = nearestNode.pos + dir[i];
                        if (newPos.x < mapMin.x || newPos.x > mapMax.x || newPos.y < mapMin.y || newPos.y > mapMax.y
                            || !IsPosWalkable(newPos)
                            || !IsPosSizeFit(newPos))
                            continue;

                        if (newPos.Equals(end[0]))
                        {
                            var endNode = new FastAStarNode()
                            {
                                pos = newPos,
                                G = nearestNode.G + 1,
                                H = 0,
                                I = lastI,
                                parentIndex = parentIndex
                            };
                            BuildPath(endNode);
                            return;
                        }

                        var hasLastDir = nearestNode.G >= 2;
                        var isTurn = hasLastDir && !lastDir.Equals(dir[i]);
                        var newNode = new FastAStarNode()
                        {
                            pos = newPos,
                            G = nearestNode.G + 1,
                            H = math.abs(end[0].x - newPos.x) + math.abs(end[0].y - newPos.y),
                            I = isTurn ? lastI + 1 : lastI,
                            parentIndex = parentIndex
                        };
                        AddOrUpdateOpenNode(newNode);
                    }
                }
                nodeCount[0] = 0;
            }

            // private int GetHeuristic(int2 pos)
            // {
            //     var delta = math.abs(end[0] - pos);
            //     return isHex ? delta.x + delta.y : math.max(delta.x, delta.y);
            // }

            private void BuildPath(FastAStarNode endPos)
            {
                while (true)
                {
                    nodes.Add(endPos.pos);
                    if (endPos.parentIndex == -1)
                    {
                        break;
                    }

                    endPos = closeList[endPos.parentIndex];
                }

                nodeCount[0] = nodes.Length;
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

            private void AddOrUpdateOpenNode(FastAStarNode node)
            {
                for (var i = 0; i < openList.Length; i++)
                {
                    if (openList[i].pos.Equals(node.pos))
                    {
                        if (IsBetterNode(node, openList[i]))
                        {
                            openList[i] = node;
                            SiftUp(i);
                            SiftDown(i);
                        }

                        return;
                    }
                }

                PushOpenNode(node);
            }

            private void PushOpenNode(FastAStarNode node)
            {
                openList.Add(node);
                SiftUp(openList.Length - 1);
            }

            private FastAStarNode PopNearestNode()
            {
                var nearestNode = openList[0];
                var lastIndex = openList.Length - 1;
                if (lastIndex == 0)
                {
                    openList.RemoveAtSwapBack(0);
                    return nearestNode;
                }

                openList[0] = openList[lastIndex];
                openList.RemoveAtSwapBack(lastIndex);
                SiftDown(0);
                return nearestNode;
            }

            private void SiftUp(int index)
            {
                while (index > 0)
                {
                    var parentIndex = (index - 1) >> 1;
                    if (!IsBetterNode(openList[index], openList[parentIndex]))
                    {
                        break;
                    }

                    SwapOpenNode(index, parentIndex);
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
                    if (rightIndex < openList.Length && IsBetterNode(openList[rightIndex], openList[leftIndex]))
                    {
                        bestIndex = rightIndex;
                    }

                    if (!IsBetterNode(openList[bestIndex], openList[index]))
                    {
                        break;
                    }

                    SwapOpenNode(index, bestIndex);
                    index = bestIndex;
                }
            }

            private void SwapOpenNode(int indexA, int indexB)
            {
                var temp = openList[indexA];
                openList[indexA] = openList[indexB];
                openList[indexB] = temp;
            }

            private bool IsBetterNode(FastAStarNode a, FastAStarNode b)
            {
                if (a.F != b.F)
                {
                    return a.F < b.F;
                }

                if (a.H != b.H)
                {
                    return a.H < b.H;
                }

                return a.I < b.I;
            }

            private int AddToCloseList(FastAStarNode node)
            {
                var mapIndex = GetMapIndex(node.pos);
                if (mapIndex == -1 || map.Length <= mapIndex || !map[mapIndex]) return -1;
                closeList[mapIndex] = node;
                return mapIndex;
            }

            private bool IsPosWalkable(int2 pos)
            {
                var mapIndex = GetMapIndex(pos);
                if (mapIndex == -1 || map.Length <= mapIndex || !map[mapIndex]) return false;
                // 已经走过的节点不可走
                if (closeList[mapIndex].G != 0)
                {
                    return false;
                }
                return true;
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