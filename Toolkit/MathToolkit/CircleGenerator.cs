using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class CircleGenerator
    {
        /// <summary>
        /// 计算在2D指定位置生成的数个圆形位置
        /// </summary>
        /// <param name="center">生成的中心位置</param>
        /// <param name="count">生成数量</param>
        /// <param name="radius">圆形半径</param>
        /// <param name="denseStacking">是否使用密堆积。默认true</param>
        /// <returns>排列好的圆形位置列表</returns>
        public static List<Vector3> GenerateCirclePositions(Vector3 center, int count, float radius, bool denseStacking = true)
        {
            List<Vector3> positions = new List<Vector3>();
            if (count <= 0) return positions;
            Vector3 averagePos = center;
            positions.Add(center);
            count--;
            if (count == 0)
            {
                return positions;
            }

            int layer = 1;
            while (count > 0)
            {
                // 每层最大容量：6 * layer
                int layerCapacity = 6 * layer;
                int currentLayerCount = Mathf.Min(layerCapacity, count);

                averagePos = denseStacking 
                    ? GenerateDenseStacking(positions, center, averagePos, radius, layer, currentLayerCount)
                    : GenerateHexLayer(positions, center, averagePos, radius, layer, currentLayerCount);

                count -= currentLayerCount;
                layer++;
            }
            averagePos = averagePos / positions.Count;
            var offset = center - averagePos;
            offset.z = 0;
            for (var i = 0; i < positions.Count; i++)
            {
                positions[i] = positions[i] + offset;
            }

            return positions;
        }

        // 密堆积算法
        private static Vector3 GenerateDenseStacking(List<Vector3> list, Vector3 center, Vector3 posSum, float radius, int layer, int count)
        {
            float layerRadius = 2 * radius * layer; 
            for (var i = 0; i < count; i++)
            {
                var isAnglePos = i % layer == 0;
                var angleIndex = Mathf.FloorToInt(i + 0.1f / layer);
                var angle = angleIndex * (Mathf.PI / 3);
                var posAngle = new Vector3(Mathf.Cos(angle) * layerRadius, Mathf.Sin(angle) * layerRadius, center.z);
                if (isAnglePos)
                {
                    list.Add(posAngle);
                    posSum = posSum + posAngle;
                }
                else
                {
                    var angleNext = angle + (Mathf.PI / 3);
                    var posAngleNext = new Vector3(Mathf.Cos(angleNext) * layerRadius, Mathf.Sin(angleNext) * layerRadius, center.z);
                    var indexInLine = i % layer;
                    var lerpValue = indexInLine * 1f / layer;
                    var pos = Vector3.Lerp(posAngle, posAngleNext, lerpValue);
                    list.Add(pos);
                    posSum = posSum + pos;
                }
            }
            return posSum;
        }

        // 圆形排列
        private static Vector3 GenerateHexLayer(List<Vector3> list, Vector3 center, Vector3 posSum, float radius, int layer, int count)
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

                list.Add(center + offset);
                posSum = posSum + center + offset;
            }
            return posSum;
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