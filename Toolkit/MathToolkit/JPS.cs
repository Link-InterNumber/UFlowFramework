using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PowerCellStudio
{
    public class Point
    {
        public int x, y;
        public float F, G, H;
        public Point parent;

        public Point(int _x, int _y)
        {
            this.x = _x;
            this.y = _y;
            F = 0;
            G = 0;
            H = 0;
        }
        
        public Point(Vector2Int pos)
        {
            this.x = pos.x;
            this.y = pos.y;
            F = 0;
            G = 0;
            H = 0;
        }

        public Vector2Int Vector2Int => new Vector2Int(x, y);
    }

    public class JPS
    {
        public HashSet<Vector2Int> map;
        private readonly List<Point> openList = new List<Point>();
        private readonly Dictionary<Vector2Int, Point> openMap = new Dictionary<Vector2Int, Point>();
        private readonly Dictionary<Vector2Int, int> openIndexMap = new Dictionary<Vector2Int, int>();
        private readonly HashSet<Vector2Int> closeList = new HashSet<Vector2Int>();
        public List<Point> GizmosListForline = new List<Point>();
        private Point destination;
        private byte[] walkMap;
        private int mapMinX;
        private int mapMinY;
        private int mapMaxX;
        private int mapMaxY;
        private int mapWidth;
        private static readonly Vector2Int Right = new Vector2Int(1, 0);
        private static readonly Vector2Int Up = new Vector2Int(0, 1);
        private static readonly Vector2Int Left = new Vector2Int(-1, 0);
        private static readonly Vector2Int Down = new Vector2Int(0, -1);
        private static readonly Vector2Int RightUp = new Vector2Int(1, 1);
        private static readonly Vector2Int RightDown = new Vector2Int(1, -1);
        private static readonly Vector2Int LeftUp = new Vector2Int(-1, 1);
        private static readonly Vector2Int LeftDown = new Vector2Int(-1, -1);

        public Point Destination
        {
            get { return destination; }
            set
            {
                if (!isWalkable(value.x, value.y))
                {
                    LinkLogger.Log("设置错误");
                    destination = null;
                }
                else
                {
                    destination = value;
                }
            }
        }

        public void initMap(IEnumerable<Vector2Int> arr)
        {
            this.map = arr.ToHashSet();
            BuildArrayMap();
        }
        
        public void initMap(HashSet<Vector2Int> arr)
        {
            this.map = arr;
            BuildArrayMap();
        }

        private void BuildArrayMap()
        {
            walkMap = null;
            mapWidth = 0;
            if (map == null || map.Count == 0)
            {
                return;
            }

            mapMinX = int.MaxValue;
            mapMinY = int.MaxValue;
            mapMaxX = int.MinValue;
            mapMaxY = int.MinValue;
            foreach (var pos in map)
            {
                if (pos.x < mapMinX) mapMinX = pos.x;
                if (pos.y < mapMinY) mapMinY = pos.y;
                if (pos.x > mapMaxX) mapMaxX = pos.x;
                if (pos.y > mapMaxY) mapMaxY = pos.y;
            }

            mapWidth = mapMaxX - mapMinX + 1;
            var mapHeight = mapMaxY - mapMinY + 1;
            walkMap = new byte[mapWidth * mapHeight];
            foreach (var pos in map)
            {
                walkMap[GetMapIndexUnchecked(pos.x, pos.y)] = 1;
            }
        }

        private float CalculateG(Point current, Vector2Int nextPos)
        {
            return current.G + CalculateMoveCost(current.x, current.y, nextPos.x, nextPos.y);
        }

        private float CalculateMoveCost(int fromX, int fromY, int toX, int toY)
        {
            var dx = Mathf.Abs(toX - fromX);
            var dy = Mathf.Abs(toY - fromY);
            var diagonal = Mathf.Min(dx, dy);
            var straight = Mathf.Max(dx, dy) - diagonal;
            return diagonal * 1.41421356237f + straight;
        }

        private float CalculateH(Vector2Int point, Point end)
        {
            var dx = Mathf.Abs(end.x - point.x);
            var dy = Mathf.Abs(end.y - point.y);
            var diagonal = Mathf.Min(dx, dy);
            var straight = Mathf.Max(dx, dy) - diagonal;
            return diagonal * 1.41421356237f + straight;
        }

        private float CalculateF(float g, float h)
        {
            return g + h;
        }

        private Point PopLeastF()
        {
            if (openList.Count == 0)
            {
                return null;
            }

            var result = openList[0];
            var resultPos = new Vector2Int(result.x, result.y);
            var lastIndex = openList.Count - 1;
            if (lastIndex == 0)
            {
                openList.RemoveAt(0);
                openMap.Remove(resultPos);
                openIndexMap.Remove(resultPos);
                return result;
            }

            openList[0] = openList[lastIndex];
            openList.RemoveAt(lastIndex);
            openMap.Remove(resultPos);
            openIndexMap.Remove(resultPos);
            openIndexMap[new Vector2Int(openList[0].x, openList[0].y)] = 0;
            SiftDown(0);
            return result;
        }

        private bool isWalkable(int x, int y)
        {
            if (walkMap == null || x < mapMinX || x > mapMaxX || y < mapMinY || y > mapMaxY)
            {
                return false;
            }

            return walkMap[GetMapIndexUnchecked(x, y)] == 1;
        }

        private bool isWalkable(Vector2Int pos)
        {
            return isWalkable(pos.x, pos.y);
        }

        private int GetMapIndexUnchecked(int x, int y)
        {
            return (y - mapMinY) * mapWidth + (x - mapMinX);
        }

        private bool LineSearch(Vector2Int current, Vector2Int dir, out Vector2Int jumpPoint)
        {
            jumpPoint = default;
            if (dir == Vector2Int.zero)
            {
                LinkLogger.Log("Error!");
                return false;
            }

            var temp = current + dir;
            while (true)
            {
                if (temp.x == destination.x && temp.y == destination.y)
                {
                    jumpPoint = temp;
                    return true;
                }

                if (!isWalkable(temp))
                {
                    return false;
                }

                if (dir.x != 0 && dir.y == 0)
                {
                    if ((!isWalkable(temp.x, temp.y + 1) && isWalkable(temp.x + dir.x, temp.y + 1) && isWalkable(temp.x + dir.x, temp.y))
                        || (!isWalkable(temp.x, temp.y - 1) && isWalkable(temp.x + dir.x, temp.y - 1) && isWalkable(temp.x + dir.x, temp.y)))
                    {
                        jumpPoint = temp;
                        return true;
                    }
                }
                else if (dir.y != 0 && dir.x == 0)
                {
                    if ((!isWalkable(temp.x + 1, temp.y) && isWalkable(temp.x + 1, temp.y + dir.y) && isWalkable(temp.x, temp.y + dir.y))
                        || (!isWalkable(temp.x - 1, temp.y) && isWalkable(temp.x - 1, temp.y + dir.y) && isWalkable(temp.x, temp.y + dir.y)))
                    {
                        jumpPoint = temp;
                        return true;
                    }
                }

                temp += dir;
            }
        }

        private void StraightSearch(Point curPoint)
        {
            var current = new Vector2Int(curPoint.x, curPoint.y);
            TryAddJumpPoint(LineSearch(current, Right, out var right), right, curPoint);
            TryAddJumpPoint(LineSearch(current, Up, out var up), up, curPoint);
            TryAddJumpPoint(LineSearch(current, Left, out var left), left, curPoint);
            TryAddJumpPoint(LineSearch(current, Down, out var down), down, curPoint);
        }

        private bool LineSearch2(Vector2Int current, Vector2Int dir, Vector2Int horizontalDir, Vector2Int verticalDir, out Vector2Int jumpPoint)
        {
            jumpPoint = default;
            if (dir == Vector2Int.zero)
            {
                LinkLogger.Log("Error");
                return false;
            }

            var temp = current + dir;
            while (true)
            {
                if (!isWalkable(temp))
                {
                    return false;
                }

                if (temp.x == destination.x && temp.y == destination.y)
                {
                    jumpPoint = temp;
                    return true;
                }

                if (HasDiagonalForcedNeighbour(temp, dir))
                {
                    jumpPoint = temp;
                    return true;
                }

                if (LineSearch(temp, horizontalDir, out _) || LineSearch(temp, verticalDir, out _))
                {
                    jumpPoint = temp;
                    return true;
                }

                temp += dir;
            }
        }

        private bool HasDiagonalForcedNeighbour(Vector2Int temp, Vector2Int dir)
        {
            if (dir.x > 0 && dir.y > 0)
            {
                return (!isWalkable(temp.x, temp.y - dir.y) && isWalkable(temp.x + 1, temp.y - dir.y) && isWalkable(temp.x + 1, temp.y))
                       || (!isWalkable(temp.x - dir.x, temp.y) && isWalkable(temp.x - dir.x, temp.y + 1) && isWalkable(temp.x, temp.y + 1));
            }

            if (dir.x > 0 && dir.y < 0)
            {
                return (!isWalkable(temp.x, temp.y - dir.y) && isWalkable(temp.x + 1, temp.y - dir.y) && isWalkable(temp.x + 1, temp.y))
                       || (!isWalkable(temp.x - dir.x, temp.y) && isWalkable(temp.x - dir.x, temp.y - 1) && isWalkable(temp.x, temp.y - 1));
            }

            if (dir.x < 0 && dir.y > 0)
            {
                return (!isWalkable(temp.x - dir.x, temp.y) && isWalkable(temp.x - dir.x, temp.y + 1) && isWalkable(temp.x, temp.y + 1))
                       || (!isWalkable(temp.x, temp.y - dir.y) && isWalkable(temp.x - 1, temp.y - dir.y) && isWalkable(temp.x - 1, temp.y));
            }

            return (!isWalkable(temp.x - dir.x, temp.y) && isWalkable(temp.x - dir.x, temp.y - 1) && isWalkable(temp.x, temp.y - 1))
                   || (!isWalkable(temp.x, temp.y - dir.y) && isWalkable(temp.x - 1, temp.y - dir.y) && isWalkable(temp.x - 1, temp.y));
        }

        private void DiagonalSearch(Point curPoint)
        {
            var current = new Vector2Int(curPoint.x, curPoint.y);
            TryAddJumpPoint(LineSearch2(current, RightUp, Right, Up, out var rightUp), rightUp, curPoint);
            TryAddJumpPoint(LineSearch2(current, RightDown, Right, Down, out var rightDown), rightDown, curPoint);
            TryAddJumpPoint(LineSearch2(current, LeftUp, Left, Up, out var leftUp), leftUp, curPoint);
            TryAddJumpPoint(LineSearch2(current, LeftDown, Left, Down, out var leftDown), leftDown, curPoint);
        }

        private Point JPS_search(Point start)
        {
            start.G = 0f;
            start.H = CalculateH(new Vector2Int(start.x, start.y), destination);
            start.F = CalculateF(start.G, start.H);
            AddOrUpdateOpenList(start);

            while (openList.Count > 0)
            {
                var curPoint = PopLeastF();
                if (curPoint == null)
                {
                    return null;
                }

                if (curPoint.x == destination.x && curPoint.y == destination.y)
                {
                    return curPoint;
                }

                closeList.Add(new Vector2Int(curPoint.x, curPoint.y));
                StraightSearch(curPoint);
                DiagonalSearch(curPoint);
            }

            return null;
        }

        private void TryAddJumpPoint(bool hasJumpPoint, Vector2Int jumpPoint, Point parent)
        {
            if (!hasJumpPoint || closeList.Contains(jumpPoint))
            {
                return;
            }

            var pos = jumpPoint;
            var g = CalculateG(parent, pos);
            if (openMap.TryGetValue(pos, out var oldPoint))
            {
                if (g >= oldPoint.G)
                {
                    return;
                }

                oldPoint.parent = parent;
                oldPoint.G = g;
                oldPoint.H = CalculateH(pos, destination);
                oldPoint.F = CalculateF(oldPoint.G, oldPoint.H);
                if (openIndexMap.TryGetValue(pos, out var index))
                {
                    SiftUp(index);
                    SiftDown(index);
                }
                return;
            }

            var h = CalculateH(pos, destination);
            AddOrUpdateOpenList(new Point(pos)
            {
                parent = parent,
                G = g,
                H = h,
                F = CalculateF(g, h)
            });
        }

        private void AddOrUpdateOpenList(Point point)
        {
            var pos = new Vector2Int(point.x, point.y);
            if (!openMap.TryGetValue(pos, out var oldPoint))
            {
                openMap[pos] = point;
                openList.Add(point);
                openIndexMap[pos] = openList.Count - 1;
                SiftUp(openList.Count - 1);
                return;
            }

            if (point.G >= oldPoint.G)
            {
                return;
            }

            oldPoint.parent = point.parent;
            oldPoint.G = point.G;
            oldPoint.H = point.H;
            oldPoint.F = point.F;
            if (openIndexMap.TryGetValue(pos, out var index))
            {
                SiftUp(index);
                SiftDown(index);
            }
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

                SwapOpenNode(index, parentIndex);
                index = parentIndex;
            }
        }

        private void SiftDown(int index)
        {
            while (true)
            {
                var leftIndex = index * 2 + 1;
                if (leftIndex >= openList.Count)
                {
                    break;
                }

                var rightIndex = leftIndex + 1;
                var bestIndex = leftIndex;
                if (rightIndex < openList.Count && IsBetter(openList[rightIndex], openList[leftIndex]))
                {
                    bestIndex = rightIndex;
                }

                if (!IsBetter(openList[bestIndex], openList[index]))
                {
                    break;
                }

                SwapOpenNode(index, bestIndex);
                index = bestIndex;
            }
        }

        private bool IsBetter(Point a, Point b)
        {
            if (!Mathf.Approximately(a.F, b.F))
            {
                return a.F < b.F;
            }

            return a.H < b.H;
        }

        private void SwapOpenNode(int indexA, int indexB)
        {
            var temp = openList[indexA];
            openList[indexA] = openList[indexB];
            openList[indexB] = temp;
            openIndexMap[new Vector2Int(openList[indexA].x, openList[indexA].y)] = indexA;
            openIndexMap[new Vector2Int(openList[indexB].x, openList[indexB].y)] = indexB;
        }

        public List<Point> GetPath(Point start)
        {
            if (!isWalkable(start.x, start.y) || destination == null)
            {
                return new List<Point>();
            }

            ClearSearchCache();
            var res = JPS_search(start);
            return BuildPath(res);
        }
        
        public List<Point> GetPath(Vector2Int start, Vector2Int des)
        {
            if (start.Equals(des)) return new List<Point>() { new Point(start) };
            if (!isWalkable(start) || !isWalkable(des))
            {
                return new List<Point>();
            }

            Destination = new Point(des);
            if (destination == null)
            {
                return new List<Point>();
            }

            ClearSearchCache();
            var res = JPS_search(new Point(start));
            return BuildPath(res);
        }

        private List<Point> BuildPath(Point res)
        {
            var path = new List<Point>();
            GizmosListForline.Clear();
            while (res != null)
            {
                path.Add(res);
                GizmosListForline.Add(res);
                res = res.parent;
            }

            path.Reverse();
            GizmosListForline.Reverse();
            ClearSearchCache();
            return path;
        }

        private void ClearSearchCache()
        {
            openList.Clear();
            openMap.Clear();
            openIndexMap.Clear();
            closeList.Clear();
        }
    }
}
