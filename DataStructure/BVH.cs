using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    
    /// <summary>
    /// 示例游戏对象
    /// </summary>
    public interface IBVHItem
    {
        public BoundingBox Bounds { get; set; }
        public Vector3 Position { get; set; }
    }

    /// <summary>
    /// 轴对齐包围盒 (AABB)
    /// </summary>
    public class BoundingBox
    {
        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }

        public Vector3 Size => Max - Min;

        public Vector3 Center => (Min + Max) / 2;

        // 计算包围盒的相交检测
        public bool Intersects(BoundingBox other)
        {
            return (Min.x <= other.Max.x && Max.x >= other.Min.x) &&
                (Min.y <= other.Max.y && Max.y >= other.Min.y) &&
                (Min.z <= other.Max.z && Max.z >= other.Min.z);
        }

        // 扩展包围盒以包含另一个包围盒
        public void Expand(BoundingBox other)
        {
            Min = Vector3.Min(Min, other.Min);
            Max = Vector3.Max(Max, other.Max);
        }
    }

    /// <summary>
    /// BVH节点基类
    /// </summary>
    public class BVHNode
    {
        // 当前节点的包围盒
        public BoundingBox Bounds { get; set; }

        // 子节点（如果是叶子节点则为null）
        public BVHNode Left { get; set; }
        public BVHNode Right { get; set; }

        // 叶子节点包含的物体列表
        public List<IBVHItem> Objects { get; set; } = new List<IBVHItem>();
    }

    /// <summary>
    /// BVH树构建器
    /// </summary>
    public class BVHTree
    {
        // 根节点
        private BVHNode root;

        // 构建BVH树
        public void Build(List<IBVHItem> objects)
        {
            if (objects == null || objects.Count == 0)
            {
                root = null;
                return;
            }
            root = BuildRecursive(objects, 0, objects.Count);
        }

        // 递归构建节点
        private BVHNode BuildRecursive(List<IBVHItem> objects, int start, int count)
        {
            var node = new BVHNode();

            // 计算当前所有物体的包围盒
            var bounds = new BoundingBox();
            for (int i = start; i < start + count; i++)
            {
                var obj = objects[i];
                bounds.Expand(obj.Bounds);
            }
            node.Bounds = bounds;

            // 终止条件：物体数量小于阈值
            if (count <= 5) // 阈值可根据需求调整
            {
                node.Objects = objects.GetRange(start, count);
                return node;
            }

            // 选择分割轴（这里简化为选择最大跨度轴）
            var axis = GetLongestAxis(node.Bounds);

            // 按中位数分割物体
            // var sorted = objects.OrderBy(o => o.Position[axis]).ToList();
            objects.Sort(start, count, new AxisComparer(axis));
            var midOffset = count / 2;
            var midIndex = start + midOffset;

            // 传递索引和计数，而不是创建新列表
            node.Left = BuildRecursive(objects, start, midOffset);
            node.Right = BuildRecursive(objects, midIndex, count - midOffset);

            return node;
        }
        
        private class AxisComparer : IComparer<IBVHItem>
        {
            private int axis;
            public AxisComparer(int axis)
            {
                this.axis = axis;
            }

            public int Compare(IBVHItem a, IBVHItem b)
            {
                return a.Position[axis].CompareTo(b.Position[axis]);
            }
        }

        // 碰撞检测入口
        public List<IBVHItem> QueryCollisions(BoundingBox queryBox)
        {
            var results = new List<IBVHItem>();
            QueryRecursive(root, queryBox, results);
            return results;
        }

        // 递归查询碰撞
        private void QueryRecursive(BVHNode node, BoundingBox queryBox, List<IBVHItem> results)
        {
            if (!node.Bounds.Intersects(queryBox)) return;

            // 叶子节点直接检测物体
            if (node.Objects != null && node.Objects.Count > 0)
            {
                foreach (var obj in node.Objects)
                {
                    if (obj.Bounds.Intersects(queryBox))
                    {
                        results.Add(obj);
                    }
                }
            }
            else
            {
                QueryRecursive(node.Left, queryBox, results);
                QueryRecursive(node.Right, queryBox, results);
            }
        }

        // 辅助方法：计算包围盒
        private BoundingBox CalculateBoundingBox(List<IBVHItem> objects)
        {
            var bounds = new BoundingBox();
            foreach (var obj in objects)
            {
                bounds.Expand(obj.Bounds);
            }
            return bounds;
        }

        // 辅助方法：获取最大跨度轴（0=X, 1=Y, 2=Z）
        private int GetLongestAxis(BoundingBox bounds)
        {
            var size = bounds.Max - bounds.Min;
            if (size.x >= size.y && size.x >= size.z) return 0;
            return size.y >= size.z ? 1 : 2;
        }
    }
}