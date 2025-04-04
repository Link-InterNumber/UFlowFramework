using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace PowerCellStudio
{
    public class AStarNode : IComparable
    {
        public static int Counter;

        public Vector2Int Pos;

        public AStarNode parent;

        public int Depth
        {
            get
            {
                var parent = this.parent;
                var depth = 1;
                while (parent != null)
                {
                    depth++;
                    parent = parent.parent;
                }

                return depth;
            }
        }

        /// <summary>
        /// 深度
        /// </summary>
        public float G;

        /// <summary>
        /// 距离终点长度
        /// </summary>
        public float H;

        /// <summary>
        /// 从起点到这里，有几次拐弯
        /// </summary>
        public float I;

        public float F => G + H + I;

        public AStarNode(Vector2Int pos, Vector2Int endPos, AStarNode parent = null)
        {
            Counter++;

            this.parent = parent;
            Pos = pos;

            G = Depth;
            H = Mathf.Abs(pos.x - endPos.x) + Mathf.Abs(pos.y - endPos.y);
            I = parent?.I ?? 0f;

            if (parent == null || parent.parent == null) return;
            if (ToVector3(parent.Pos - Pos).normalized != ToVector3(parent.parent.Pos - parent.Pos).normalized)
            {
                I += 1f;
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
                list.Insert(0, parent.Pos);
                parent = parent.parent;
            }

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
            return base.GetHashCode();
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
        public HashSet<AStarNode> openList = new HashSet<AStarNode>();
        public HashSet<AStarNode> closeList = new HashSet<AStarNode>();

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
            AddToOpenList(from, to, parent);

            return NextNode(to);
        }

        private List<Vector2Int> NextNode(Vector2Int to)
        {
            if (!openList.Any()) return null;

            var node = GetNextNode(to);
            if (node.Pos == to) return node.ToList();

            return NextNode(to);
        }

        public AStarNode GetNextNode(Vector2Int to)
        {
            if (!openList.Any()) return null;

//            var nearest = openList.OrderBy(o => o.F).First(); //O(nlogn)
            var nearest = openList.Min(); //O(n)
            if (nearest.Pos == to) return nearest;

            openList.Remove(nearest);
            closeList.Add(nearest);

            if (IsHex)
            {
                directions = nearest.Pos.y % 2 == 0 ? evenRowDir : oddRowDir;
            }

            foreach (var dir in directions)
            {
                var nextPos = nearest.Pos + dir;
                if (checkEndInGround && nextPos == to && !IsValidPosForCard(nextPos)) continue;

                if (closeList.Any(o => o.Pos == nextPos) ||
                    (nextPos != to && !IsValidPosForCard(nextPos))) continue;

                AddToOpenList(nextPos, to, nearest);
            }

            return nearest;
        }

        private bool IsValidPosForCard(Vector2Int pos)
        {
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
            var node = openList.FirstOrDefault(o => o.Pos == from);
            if (node == null)
            {
                //TODO: new AStarNode 改用Pool
                node = new AStarNode(from, to, parent);
                openList.Add(node);
            }
            else if (parent != null && node.parent != parent)
            {
                if (parent.G + 1 < node.G) node.parent = parent;
            }
        }
    }
}