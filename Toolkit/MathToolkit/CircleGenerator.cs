using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class CircleGenerator
    {
        public static List<Vector3> GenerateCirclePositions(Vector3 center, int count, float radius)
        {
            List<Vector3> positions = new List<Vector3>();

            if (count <= 0) return positions;

            // 添加中心点（当数量为奇数时）
            bool hasCenter = count % 2 != 0;
            if (hasCenter)
            {
                positions.Add(center);
                count--;
            }

            // 分层生成六边形排列
            int layer = 1;
            while (count > 0)
            {
                // 每层最大容量：6 * layer
                int layerCapacity = 6 * layer;
                int currentLayerCount = Mathf.Min(layerCapacity, count);

                // 生成层的位置
                GenerateHexLayer(positions, center, radius, layer, currentLayerCount);

                count -= currentLayerCount;
                layer++;
            }

            return positions;
        }

        private static void GenerateHexLayer(List<Vector3> list, Vector3 center, float radius, int layer, int count)
        {
            float layerRadius = 2 * radius * layer;
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                // 计算六边形层坐标
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * layerRadius,
                    Mathf.Sin(angle) * layerRadius,
                    0
                );

                // 添加对称点对（保证平均位置）
                list.Add(center + offset);
                list.Add(center - offset);

                // 跳过重复生成
                i++;
            }

            // 移除多余的对称点
            if (count % 2 != 0)
            {
                list.RemoveRange(list.Count - 2, 2);
            }
        }

        // 验证平均位置的测试方法
        public static void Test()
        {
            Vector3 center = new Vector3(5, 5);
            float radius = 0.5f;
            int testCount = 7;

            List<Vector3> positions = GenerateCirclePositions(center, testCount, radius);

            // 计算平均位置
            Vector3 sum = Vector3.zero;
            foreach (Vector3 pos in positions)
            {
                sum += pos;
                Debug.Log(pos);
            }

            Vector3 average = sum / positions.Count;
            Debug.Log($"Average Position: {average} (Should be {center})");
        }
    }
}