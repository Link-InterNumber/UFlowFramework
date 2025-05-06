using System.Collections.Generic;
using System.Linq;

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

        // 计算包围盒的相交检测
        public bool Intersects(BoundingBox other)
        {
            return (Min.X <= other.Max.X && Max.X >= other.Min.X) &&
                (Min.Y <= other.Max.Y && Max.Y >= other.Min.Y) &&
                (Min.Z <= other.Max.Z && Max.Z >= other.Min.Z);
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
            root = BuildRecursive(objects);
        }

        // 递归构建节点
        private BVHNode BuildRecursive(List<IBVHItem> objects)
        {
            var node = new BVHNode();
            
            // 计算当前所有物体的包围盒
            node.Bounds = CalculateBoundingBox(objects);

            // 终止条件：物体数量小于阈值
            if (objects.Count <= 5) // 阈值可根据需求调整
            {
                node.Objects = objects;
                return node;
            }

            // 选择分割轴（这里简化为选择最大跨度轴）
            var axis = GetLongestAxis(node.Bounds);

            // 按中位数分割物体
            var sorted = objects.OrderBy(o => o.Position[axis]).ToList();
            var mid = sorted.Count / 2;

            // 递归构建子树
            node.Left = BuildRecursive(sorted.Take(mid).ToList());
            node.Right = BuildRecursive(sorted.Skip(mid).ToList());

            return node;
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
            if (node.Objects != null)
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
            if (size.X >= size.Y && size.X >= size.Z) return 0;
            return size.Y >= size.Z ? 1 : 2;
        }
    }
}