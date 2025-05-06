using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class VirtualGrid
    {
        public float cellSize = 1f; // 每个网格单元的大小
        public Vector2 offset = new Vector2(0f, 0f);

        private Dictionary<Vector2Int, List<IToVector2>> grid = new Dictionary<Vector2Int, List<IToVector2>>(); // 存储网格单元及其包含的对象

        /// <summary>
        /// 将对象添加到网格中
        /// </summary>
        /// <param name="obj">要添加的对象</param>
        public void AddObject(IToVector2 obj)
        {
            Vector2Int cellKey = GetCellKey(obj.ToVector2());
            if (!grid.ContainsKey(cellKey))
            {
                grid[cellKey] = new List<IToVector2>();
            }
            grid[cellKey].Add(obj);
        }

        /// <summary>
        /// 从网格中移除对象
        /// </summary>
        /// <param name="obj">要移除的对象</param>
        public void RemoveObject(IToVector2 obj)
        {
            Vector2Int cellKey = GetCellKey(obj.ToVector2());
            if (grid.ContainsKey(cellKey))
            {
                grid[cellKey].Remove(obj);
                if (grid[cellKey].Count == 0)
                {
                    grid.Remove(cellKey); // 如果单元格为空，则删除该单元格
                }
            }
        }

        /// <summary>
        /// 更新对象在网格中的位置
        /// </summary>
        /// <param name="obj">要更新的对象</param>
        public void UpdateObject(IToVector2 obj)
        {
            RemoveObject(obj); // 先从旧单元格移除
            AddObject(obj);    // 再添加到新单元格
        }

        /// <summary>
        /// 获取指定位置的单元格中的对象
        /// </summary>
        /// <param name="position">要查询的位置</param>
        /// <returns>单元格中的对象列表</returns>
        public List<IToVector2> GetObjectsAt(Vector2 position)
        {
            Vector2Int cellKey = GetCellKey(position);
            if (grid.ContainsKey(cellKey))
            {
                return grid[cellKey];
            }
            return new List<IToVector2>();
        }

        /// <summary>
        /// 根据位置计算单元格的键
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>单元格的键</returns>
        private Vector2Int GetCellKey(Vector2 position)
        {
            var calPos = position - offset;
            int cellX = Mathf.FloorToInt(calPos.x / cellSize);
            int cellY = Mathf.FloorToInt(calPos.y / cellSize);
            return new Vector2Int(cellX, cellY);
        }

        /// <summary>
        /// 调试：绘制网格
        /// </summary>
        private void OnDrawGizmos()
        {
            var originColor = Gizmos.color;
            Gizmos.color = Color.gray;
            for (float x = offset.x - 10; x < offset.x + 10; x += cellSize)
            {
                for (float y = offset.y - 10; y < offset.y + 10; y += cellSize)
                {
                    Gizmos.DrawLine(new Vector3(x, offset.y - 10, 0), new Vector3(x, offset.y + 10, 0));
                    Gizmos.DrawLine(new Vector3(offset.x - 10, y, 0), new Vector3(offset.x + 10, y, 0));
                }
            }
            Gizmos.color = originColor;
        }
    }
}