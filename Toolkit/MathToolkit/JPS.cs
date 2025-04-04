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
        private int Size;
        public HashSet<Vector2Int> map;
        // public HashSet<Vector2Int> grounds = new HashSet<Vector2Int>();
        private List<Point> openList = new List<Point>();
        private List<Point> closeList = new List<Point>();
        public List<Point> GizmosListForline = new List<Point>();
        private Point destination;

        public Point Destination
        {
            get { return destination; }
            set
            {
                var pos = new Vector2Int(value.x, value.y);
                if (!map.Contains(pos))
                {
                    LinkLog.Log("设置错误");
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
        }
        
        public void initMap(HashSet<Vector2Int> arr)
        {
            this.map = arr;
        }

        float CalculateG(Point start, Point tarpoint)
        {
            float tempG = Vector2.Distance(new Vector2(start.x, start.y), new Vector2(tarpoint.x, tarpoint.y));
            float parentG = tarpoint.parent == null ? 0 : tarpoint.parent.G;
            float g = tempG + parentG;
            return g;
        } //计算G值

        float CalculateH(Point point, Point end)
        {
            return (Mathf.Abs(end.x - point.x) + Mathf.Abs(end.y - point.y)); //曼哈顿距离
        } //计算H值

        float CalculateF(Point point)
        {
            return point.G + point.H;
        } //计算F值

        Point FindleastF()
        {
            if (openList.Count > 0)
            {
                Point curPoint = openList[0];
                foreach (Point point in openList)
                {
                    if (point.F < curPoint.F)
                    {
                        curPoint = point;
                    }
                }

                return curPoint;
            }

            return null;
        } //通过遍历去找到最小的F

        private bool isWalkable(int x, int y)
        {
            var pos = new Vector2Int(x, y);
            if (!map.Contains(pos))
            {
                return false;
            }
            return true;
        }

        private Point LineSearch(Point current, Vector2Int dir) //直线搜索
        {
            if (dir.magnitude == 0)
            {
                LinkLog.Log("Error!");
                return null;
            }

            Point temp = new Point(current.x + (int) dir.x, current.y + (int) dir.y);
            while (true)
            {
                //跳点定义①：终点是跳点
                if (temp.x == destination.x && temp.y == destination.y)
                {
                    return temp;
                }

                if (!isWalkable(temp.x, temp.y)) //一旦遇到障碍物或超出地图范围就退出循环
                {
                    break;
                }

                if (dir.x != 0 && dir.y == 0) //沿X方向前进时的跳点判断
                {
                    //跳点定义②：有强迫邻居的点是跳点
                    if ((!isWalkable(temp.x, temp.y + 1) && isWalkable(temp.x + dir.x, temp.y + 1) &&
                         isWalkable(temp.x + dir.x, temp.y))
                        || (!isWalkable(temp.x, temp.y - 1) && isWalkable(temp.x + dir.x, temp.y - 1) &&
                            isWalkable(temp.x + dir.x, temp.y)))
                    {
                        //LinkLog.Log("X轴的跳点");
                        return temp; //该点就是跳点
                    }
                }

                if (dir.y != 0 && dir.x == 0) //沿Y方向前进时的跳点判断
                {
                    if ((!isWalkable(temp.x + 1, temp.y) && isWalkable(temp.x + 1, temp.y + dir.y) &&
                         isWalkable(temp.x, temp.y + dir.y))
                        || (!isWalkable(temp.x - 1, temp.y) && isWalkable(temp.x - 1, temp.y + dir.y) &&
                            isWalkable(temp.x, temp.y + dir.y)))
                    {
                        //LinkLog.Log("Y轴的跳点");
                        return temp; //该点就是跳点
                    }
                }

                temp = new Point(temp.x + (int) dir.x, temp.y + (int) dir.y);
                //LinkLog.Log(temp.x + " " + temp.y);
            }

            return null;
        }

        Point isInList(List<Point> list, Point point)
        {
            foreach (var p in list)
            {
                if (p.x == point.x && p.y == point.y)
                    return p;
            }

            return null;
        }

        private void StraightSearch(Point _curPoint) //以当前点的4条直线搜索
        {
            Point ans1 = LineSearch(_curPoint, new Vector2Int(1, 0));
            if (ans1 != null && isInList(closeList, ans1) == null)
            {
                ans1.parent = _curPoint;
                ans1.G = CalculateG(ans1, _curPoint);
                ans1.H = CalculateH(ans1, destination);
                ans1.F = CalculateF(ans1);
                openList.Add(ans1);
            }

            Point ans2 = LineSearch(_curPoint, new Vector2Int(0, 1));
            if (ans2 != null && isInList(closeList, ans2) == null)
            {
                ans2.parent = _curPoint;
                ans2.G = CalculateG(ans2, _curPoint);
                ans2.H = CalculateH(ans2, destination);
                ans2.F = CalculateF(ans2);
                openList.Add(ans2);
            }

            Point ans3 = LineSearch(_curPoint, new Vector2Int(-1, 0));
            if (ans3 != null && isInList(closeList, ans3) == null)
            {
                ans3.parent = _curPoint;
                ans3.G = CalculateG(ans3, _curPoint);
                ans3.H = CalculateH(ans3, destination);
                ans3.F = CalculateF(ans3);
                openList.Add(ans3);
            }

            Point ans4 = LineSearch(_curPoint, new Vector2Int(0, -1));
            if (ans4 != null && isInList(closeList, ans4) == null)
            {
                ans4.parent = _curPoint;
                ans4.G = CalculateG(ans4, _curPoint);
                ans4.H = CalculateH(ans4, destination);
                ans4.F = CalculateF(ans4);
                openList.Add(ans4);
            }
        }

        private Point LineSearch2(Point _curPoint, Vector2Int dir) //斜向搜索
        {
            if (dir.magnitude == 0)
            {
                LinkLog.Log("Error");
                return null;
            }

            Point temp = new Point(_curPoint.x + (int) dir.x, _curPoint.y + (int) dir.y);
            if (!isWalkable(temp.x, temp.y)) return null;
            if (dir.x > 0 && dir.y > 0) //往右上方搜索
            {
                //跳点定义②：有强迫邻居的点是跳点
                if ((!isWalkable(temp.x, temp.y - dir.y) && isWalkable(temp.x + 1, temp.y - dir.y) &&
                     isWalkable(temp.x + 1, temp.y))
                    || (!isWalkable(temp.x - dir.x, temp.y) && isWalkable(temp.x - dir.x, temp.y + 1) &&
                        isWalkable(temp.x, temp.y + 1)))
                {
                    return temp;
                }

                //跳点定义③：沿当前方向的水平和垂直分量有满足定义①和②的点时跳点
                Point SuspiciousJP = LineSearch(temp, new Vector2Int(dir.x, 0));
                Point SuspiciousJP2 = LineSearch(temp, new Vector2Int(0, dir.y));
                if (SuspiciousJP != null || SuspiciousJP2 != null)
                {
                    return temp;
                }
            }
            else if (dir.x > 0 && dir.y < 0) //沿着右下方搜索
            {
                if ((!isWalkable(temp.x, temp.y - dir.y) && isWalkable(temp.x + 1, temp.y - dir.y) &&
                     isWalkable(temp.x + 1, temp.y))
                    || (!isWalkable(temp.x - dir.x, temp.y) && isWalkable(temp.x - dir.x, temp.y - 1) &&
                        isWalkable(temp.x, temp.y - 1)))
                {
                    return temp;
                }

                //跳点定义③：沿当前方向的水平和垂直分量有满足定义①和②的点时跳点
                Point SuspiciousJP = LineSearch(temp, new Vector2Int(dir.x, 0));
                Point SuspiciousJP2 = LineSearch(temp, new Vector2Int(0, dir.y));
                if (SuspiciousJP != null || SuspiciousJP2 != null)
                {
                    return temp;
                }
            }
            else if (dir.x < 0 && dir.y > 0) //向左上方搜索
            {
                if ((!isWalkable(temp.x - dir.x, temp.y) && isWalkable(temp.x - dir.x, temp.y + 1) &&
                     isWalkable(temp.x, temp.y + 1))
                    || (!isWalkable(temp.x, temp.y - dir.y) && isWalkable(temp.x - 1, temp.y - dir.y) &&
                        isWalkable(temp.x - 1, temp.y)))
                {
                    return temp;
                }

                Point SuspiciousJP = LineSearch(temp, new Vector2Int(dir.x, 0));
                Point SuspiciousJP2 = LineSearch(temp, new Vector2Int(0, dir.y));
                if (SuspiciousJP != null || SuspiciousJP2 != null)
                {
                    return temp;
                }
            }
            else if (dir.x < 0 && dir.y < 0)
            {
                if ((!isWalkable(temp.x - dir.x, temp.y) && isWalkable(temp.x - dir.x, temp.y - 1) &&
                     isWalkable(temp.x, temp.y - 1))
                    || (!isWalkable(temp.x, temp.y - dir.y) && isWalkable(temp.x - 1, temp.y - dir.y) &&
                        isWalkable(temp.x - 1, temp.y)))
                {
                    return temp;
                }

                Point SuspiciousJP = LineSearch(temp, new Vector2Int(dir.x, 0));
                Point SuspiciousJP2 = LineSearch(temp, new Vector2Int(0, dir.y));
                if (SuspiciousJP != null || SuspiciousJP2 != null)
                {
                    return temp;
                }
            }

            return null;
        }

        private void DiagonalSearch(Point _curPoint)
        {
            Point ans1 = LineSearch2(_curPoint, new Vector2Int(1, 1));
            if (ans1 != null && isInList(closeList, ans1) == null)
            {
                ans1.parent = _curPoint;
                ans1.G = CalculateG(ans1, _curPoint);
                ans1.H = CalculateH(ans1, destination);
                ans1.F = CalculateF(ans1);
                openList.Add(ans1);
            }

            Point ans2 = LineSearch2(_curPoint, new Vector2Int(1, -1));
            if (ans2 != null && isInList(closeList, ans2) == null)
            {
                ans2.parent = _curPoint;
                ans2.G = CalculateG(ans2, _curPoint);
                ans2.H = CalculateH(ans2, destination);
                ans2.F = CalculateF(ans2);
                openList.Add(ans2);
            }

            Point ans3 = LineSearch2(_curPoint, new Vector2Int(-1, 1));
            if (ans3 != null && isInList(closeList, ans3) == null)
            {
                ans3.parent = _curPoint;
                ans3.G = CalculateG(ans3, _curPoint);
                ans3.H = CalculateH(ans3, destination);
                ans3.F = CalculateF(ans3);
                openList.Add(ans3);
            }

            Point ans4 = LineSearch2(_curPoint, new Vector2Int(-1, -1));
            if (ans4 != null && isInList(closeList, ans4) == null)
            {
                ans4.parent = _curPoint;
                ans4.G = CalculateG(ans4, _curPoint);
                ans4.H = CalculateH(ans4, destination);
                ans4.F = CalculateF(ans4);
                openList.Add(ans4);
            }
        }

        private Point JPS_search(Point start)
        {
            openList.Add(start);
            while (openList.Count > 0)
            {
                Point curPoint = FindleastF();
                openList.Remove(curPoint);
                //以当前点开始直线搜索寻找跳点
                StraightSearch(curPoint);
                DiagonalSearch(curPoint);
                closeList.Add(curPoint);
                Point resPoint = isInList(openList, destination); //如果目标点在openList中，则说明找到了路径
                if (resPoint != null)
                {
                    return resPoint;
                }
            }

            return null;
        }

        public List<Point> GetPath(Point start)
        {
            if (!map.Contains(new Vector2Int(start.x, start.y)))
                return new List<Point>();
            Point res = JPS_search(start);
            List<Point> path = new List<Point>();
            while (res != null)
            {
                path.Add(res);
                GizmosListForline.Add(res);
                res = res.parent;
            }

            openList.Clear();
            closeList.Clear();
            return path;
        }
        
        public List<Point> GetPath(Vector2Int start, Vector2Int des)
        {
            if (start.Equals(des)) return new List<Point>() { new Point(start)};
            var startPos = new Point(start);
            if (!map.Contains(new Vector2Int(start.x, start.y)))
                return new List<Point>();
            Destination = new Point(des);
            Point res = JPS_search(startPos);
            List<Point> path = new List<Point>();
            while (res != null)
            {
                path.Add(res);
                GizmosListForline.Add(res);
                res = res.parent;
            }

            openList.Clear();
            closeList.Clear();
            return path;
        }
    }
}