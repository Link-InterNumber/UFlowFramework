using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class AStarNode : IComparable
    {
        public static int Counter;

        public Vector2Int Pos;

        public AStarNode parent;

        /// <summary>
        /// 深度
        /// </summary>
        public int G;

        /// <summary>
        /// 距离终点长度
        /// </summary>
        public int H;

        /// <summary>
        /// 从起点到这里，有几次拐弯
        /// </summary>
        public int I;

        public int F => G + H + I;

        public AStarNode(Vector2Int pos, Vector2Int endPos, AStarNode parent = null)
        {
            Counter++;

            this.parent = parent;
            Pos = pos;

            G = parent == null ? 1 : parent.G + 1;
            H = Mathf.Abs(pos.x - endPos.x) + Mathf.Abs(pos.y - endPos.y);
            I = parent?.I ?? 0;

            if (parent == null || parent.parent == null) return;
            if ((parent.Pos - Pos) != (parent.parent.Pos - parent.Pos))
            {
                I += 1;
            }
        }

        private Vector3 ToVector3(Vector2Int pos)
        {
            return new Vector3(pos.x, pos.y);
        }

        public List<Vector2Int> ToList()
        {
            var list = new List<Vector2Int>();
            var parent = this;
            while (parent != null)
            {
                list.Add(parent.Pos);
                parent = parent.parent;
            }
            list.Reverse();
            return list;
        }

        public override bool Equals(object obj)
        {
            var node = obj as AStarNode;
            if (node == null) return false;

            if (Pos != node.Pos) return false;
            if (parent != node.parent) return false;
            if (!Mathf.Approximately(G, node.G)) return false;
            if (!Mathf.Approximately(H, node.H)) return false;
            if (!Mathf.Approximately(I, node.I)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return Pos.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            var other = obj as AStarNode;
            if (other == null) throw new ArgumentException("Object is not a AStarNode");

            return F.CompareTo(other.F);
        }
    }

    public class AStar
    {
        public Vector2Int[] directions = new[]
        {
            Vector2Int.up,
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.down,

            //斜方向
            Vector2Int.left + Vector2Int.up,
            Vector2Int.right + Vector2Int.up,
            Vector2Int.left + Vector2Int.down,
            Vector2Int.right + Vector2Int.down,
        };

        public bool checkEndInGround = true; //检查终点是否在地图上
        public Vector2Int cardSize = Vector2Int.one;
        public HashSet<Vector2Int> grounds = new HashSet<Vector2Int>();
        public Dictionary<Vector2Int, AStarNode> openList = new Dictionary<Vector2Int, AStarNode>();
        public List<AStarNode> orderOpenList = new List<AStarNode>();
        private readonly Dictionary<Vector2Int, int> _openHeapIndexMap = new Dictionary<Vector2Int, int>();
        public HashSet<Vector2Int> closeList = new HashSet<Vector2Int>();

        private static readonly Vector2Int[] evenRowDir = new[]
        {
            Vector2Int.up,
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.down,

            //斜方向
            Vector2Int.left + Vector2Int.up,
            // Vector2Int.right + Vector2Int.up,
            Vector2Int.left + Vector2Int.down,
            // Vector2Int.right + Vector2Int.down,
        };

        private static readonly Vector2Int[] oddRowDir = new[]
        {
            Vector2Int.up,
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.down,

            //斜方向
            // Vector2Int.left + Vector2Int.up,
            Vector2Int.right + Vector2Int.up,
            // Vector2Int.left + Vector2Int.down,
            Vector2Int.right + Vector2Int.down,
        };

        public bool IsHex = false;

        public AStar()
        {
        }

        public AStar(IEnumerable<Vector2Int> tiles)
        {
            foreach (var vector2Int in tiles)
            {
                this.grounds.Add(vector2Int);
            }
        }

        public void SetGround(IEnumerable<Vector2Int> tiles, Vector2Int cardSizeV)
        {
            this.cardSize = cardSizeV;
            this.grounds.Clear();
            foreach (var vector2Int in tiles)
            {
                this.grounds.Add(vector2Int);
            }
        }

        public static List<Vector2Int> Path(List<Vector2Int> grounds, Vector2Int from, Vector2Int to)
        {
            var astar = new AStar(grounds);
            return astar.Path(from, to);
        }

        public List<Vector2Int> Path(Vector2Int from, Vector2Int to, AStarNode parent = null)
        {
            openList.Clear();
            closeList.Clear();
            orderOpenList.Clear();
            _openHeapIndexMap.Clear();
            AddToOpenList(from, to, parent);

            return NextNode(to);
        }

        private List<Vector2Int> NextNode(Vector2Int to)
        {
            while (openList.Count > 0)
            {
                var node = GetNextNode(to);
                if (node.Pos == to)
                {
                    return node.ToList();
                }
            }

            return null;
        }

        private AStarNode GetNextNode(Vector2Int to)
        {
            // if (openList.Count == 0 || orderOpenList.Count == 0) return null;

//            var nearest = openList.OrderBy(o => o.F).First(); //O(nlogn)
            var nearest = PopOpenHeap();
            openList.Remove(nearest.Pos);
            if (nearest.Pos == to) return nearest;

            closeList.Add(nearest.Pos);

            if (IsHex)
            {
                directions = nearest.Pos.y % 2 == 0 ? evenRowDir : oddRowDir;
            }

            foreach (var dir in directions)
            {
                var nextPos = nearest.Pos + dir;
                if (checkEndInGround && nextPos == to && !IsValidPosForCard(nextPos)) continue;

                if (closeList.Contains(nextPos) ||
                    (nextPos != to && !IsValidPosForCard(nextPos))) continue;

                AddToOpenList(nextPos, to, nearest);
            }

            return nearest;
        }

        private bool IsValidPosForCard(Vector2Int pos)
        {
            if (cardSize == Vector2Int.one)
            {
                return grounds.Contains(pos);
            }
            for (var x = 0; x < cardSize.x; x++)
            {
                for (var y = 0; y < cardSize.y; y++)
                {
                    if (!grounds.Contains(new Vector2Int(pos.x + x, pos.y + y)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void AddToOpenList(Vector2Int from, Vector2Int to, AStarNode parent)
        {
            if (!openList.TryGetValue(from, out var node))
            {
                node = new AStarNode(from, to, parent);
                openList.Add(from, node);
                PushOpenHeap(node);
            }
            else if (parent != null && node.parent != parent)
            {
                if (parent.G + 1 < node.G) 
                {
                    node.parent = parent;
                    node.G = parent.G + 1;
                    node.I = parent.I;
                    if (parent.parent != null && (parent.Pos - node.Pos) != (parent.parent.Pos - parent.Pos))
                    {
                        node.I += 1;
                    }
                    if (_openHeapIndexMap.TryGetValue(node.Pos, out var heapIndex))
                    {
                        SiftUp(heapIndex);
                        SiftDown(heapIndex);
                    }
                }
            }
        }

        private void PushOpenHeap(AStarNode node)
        {
            orderOpenList.Add(node);
            var index = orderOpenList.Count - 1;
            _openHeapIndexMap[node.Pos] = index;
            SiftUp(index);
        }

        private AStarNode PopOpenHeap()
        {
            var result = orderOpenList[0];
            var lastIndex = orderOpenList.Count - 1;
            _openHeapIndexMap.Remove(result.Pos);
            if (lastIndex == 0)
            {
                orderOpenList.RemoveAt(0);
                return result;
            }

            orderOpenList[0] = orderOpenList[lastIndex];
            orderOpenList.RemoveAt(lastIndex);
            _openHeapIndexMap[orderOpenList[0].Pos] = 0;
            SiftDown(0);
            return result;
        }

        private void SiftUp(int index)
        {
            while (index > 0)
            {
                var parentIndex = (index - 1) >> 1;
                if (!IsBetter(orderOpenList[index], orderOpenList[parentIndex]))
                {
                    break;
                }

                SwapOpenHeap(index, parentIndex);
                index = parentIndex;
            }
        }

        private void SiftDown(int index)
        {
            while (true)
            {
                var leftIndex = index * 2 + 1;
                if (leftIndex >= orderOpenList.Count)
                {
                    break;
                }

                var rightIndex = leftIndex + 1;
                var bestIndex = leftIndex;
                if (rightIndex < orderOpenList.Count && IsBetter(orderOpenList[rightIndex], orderOpenList[leftIndex]))
                {
                    bestIndex = rightIndex;
                }

                if (!IsBetter(orderOpenList[bestIndex], orderOpenList[index]))
                {
                    break;
                }

                SwapOpenHeap(index, bestIndex);
                index = bestIndex;
            }
        }

        private bool IsBetter(AStarNode a, AStarNode b)
        {
            if (a.F != b.F)
            {
                return a.F < b.F;
            }

            return a.H < b.H;
        }

        private void SwapOpenHeap(int indexA, int indexB)
        {
            var temp = orderOpenList[indexA];
            orderOpenList[indexA] = orderOpenList[indexB];
            orderOpenList[indexB] = temp;
            _openHeapIndexMap[orderOpenList[indexA].Pos] = indexA;
            _openHeapIndexMap[orderOpenList[indexB].Pos] = indexB;
        }
    }
}