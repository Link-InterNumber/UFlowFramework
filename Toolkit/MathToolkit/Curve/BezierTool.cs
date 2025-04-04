using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public static class Bezier
    {
        /// <summary>
        /// 根据T值，计算二次贝塞尔曲线上面相对应的点
        /// </summary>
        /// <param name="t"></param>T值（0-1）
        /// <param name="p0"></param>起始点
        /// <param name="p1"></param>控制点
        /// <param name="p2"></param>目标点
        /// <returns></returns>根据T值计算出来的贝赛尔曲线点
        public static Vector3 CalcBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2) {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;

            Vector3 p = uu * p0;
            p += 2 * u * t * p1;
            p += tt * p2;

            return p;
        }

        /// <summary>
        /// 根据T值，计算三次贝塞尔曲线上面相对应的点
        /// </summary>
        /// <param name="t"></param>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns></returns>
        public static Vector3 CalcBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector3 p = uuu * p0;
            p += 3 * uu * t * p1;
            p += 3 * u * tt * p2;
            p += ttt * p3;

            return p;
        }

        /// <summary>
        /// 计算任意数量控制点的贝塞尔曲线
        /// </summary>
        /// <param name="t"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Vector3 CalcBezierPoint(float t, List<Vector3> points) {
            var pointCount = points.Count;
            if (pointCount == 3) {
                return CalcBezierPoint(t, points[0], points[1], points[2]);
            }
            else if (pointCount == 4) {
                return CalcBezierPoint(t, points[0], points[1], points[2], points[3]);
            }
            else {
                Debug.LogWarning("Bezier now can not over 4 points");
                return Vector3.zero;
            }
        }


        /// <summary>
        /// 获取存储贝塞尔曲线采样点
        /// </summary>
        /// <param name="startPoint"></param>起始点
        /// <param name="controlPoint"></param>控制点
        /// <param name="endPoint"></param>目标点
        /// <param name="segmentNum"></param>采样点的数量(最小2，即起点和终点)
        /// <returns></returns>存储贝塞尔曲线点的数组
        public static Vector3[] SampleBeizerPath(Vector3 startPoint, Vector3 controlPoint, Vector3 endPoint, int segmentNum)
        {
            segmentNum = Mathf.Max(2, segmentNum);
            Vector3[] path = new Vector3[segmentNum];
            for (int i = 0; i <= segmentNum; i++) 
            {
                float t = (float) i / (float) segmentNum;
                Vector3 point = CalcBezierPoint(t, startPoint, controlPoint, endPoint);
                path[i] = point;
                // Debug.Log (path[i - 1]);
            }
            return path;
        }
            
        private static int Precision(List<Vector2> points, float uniDistance = 0.08f)
        {
            var result = 0;
            for (int i = 0; i < points.Count - 1; i++)
            {
                var delta = points[i] - points[i + 1];
                result += (int)(delta.magnitude / uniDistance);
            }

            return result;
        }

        public static Vector2[] Path(List<Vector2> poss)
        {
            var precision = Bezier.Precision(poss);
            return Path(poss, precision);
        }

        /// <summary>
        /// 计算Bezier曲线上的点
        /// </summary>
        /// <param name="poss">贝塞尔曲线控制点坐标</param>
        /// <param name="precision">精度，需要计算的该条贝塞尔曲线上的点的数目</param>
        /// <returns>该条贝塞尔曲线上的点（二维坐标）</returns>
        public static Vector2[] Path(List<Vector2> poss, int precision) {

            //维度，坐标轴数（二维坐标，三维坐标...）
            var dimersion = 2;

            //贝塞尔曲线控制点数（阶数）
            var number = poss.Count;

            //控制点数不小于 2 ，至少为二维坐标系
            if (number < 2 || dimersion < 2)
                return null;

            var result = new Vector2[precision];

            //计算杨辉三角
            int[] mi = new int[number];
            mi[0] = mi[1] = 1;
            for (int i = 3; i <= number; i++) {

                int[] t = new int[i - 1];
                for (int j = 0; j < t.Length; j++) {
                    t[j] = mi[j];
                }

                mi[0] = mi[i - 1] = 1;
                for (int j = 0; j < i - 2; j++) {
                    mi[j + 1] = t[j] + t[j + 1];
                }
            }

            //计算坐标点
            for (int i = 0; i < precision; i++) {
                float t = (float) i / precision;
                var point = new Vector2();
                result[i] = point;

                for (int j = 0; j < dimersion; j++) {
                    float temp = 0.0f;
                    for (int k = 0; k < number; k++) {
                        temp += (float)(Mathf.Pow(1 - t, number - k - 1) * poss[k][j] * Mathf.Pow(t, k) * mi[k]);
                    }
                    result[i][j] = temp;
                }
            }

            return result;
        }
        
        private static int Precision(List<Vector3> points, float uniDistance = 0.08f)
        {
            var result = 0;
            for (int i = 0; i < points.Count - 1; i++)
            {
                var delta = points[i] - points[i + 1];
                result += (int)(delta.magnitude / uniDistance);
            }

            return result;
        }

        public static Vector3[] Path(List<Vector3> poss)
        {
            var precision = Bezier.Precision(poss);
            return Path(poss, precision);
        }

        /// <summary>
        /// 计算Bezier曲线上的点
        /// </summary>
        /// <param name="poss">贝塞尔曲线控制点坐标</param>
        /// <param name="precision">精度，需要计算的该条贝塞尔曲线上的点的数目</param>
        /// <returns>该条贝塞尔曲线上的点（二维坐标）</returns>
        public static Vector3[] Path(List<Vector3> poss, int precision) {

            //维度，坐标轴数（二维坐标，三维坐标...）
            var dimersion = 3;

            //贝塞尔曲线控制点数（阶数）
            var number = poss.Count;

            //控制点数不小于 2 ，至少为二维坐标系
            if (number < 2 || dimersion < 2)
                return null;

            var result = new Vector3[precision];

            //计算杨辉三角
            int[] mi = new int[number];
            mi[0] = mi[1] = 1;
            for (int i = 3; i <= number; i++) {

                int[] t = new int[i - 1];
                for (int j = 0; j < t.Length; j++) {
                    t[j] = mi[j];
                }

                mi[0] = mi[i - 1] = 1;
                for (int j = 0; j < i - 2; j++) {
                    mi[j + 1] = t[j] + t[j + 1];
                }
            }

            //计算坐标点
            for (int i = 0; i < precision; i++) {
                float t = (float) i / precision;
                var point = new Vector3();
                result[i] = point;

                for (int j = 0; j < dimersion; j++) {
                    float temp = 0.0f;
                    for (int k = 0; k < number; k++) {
                        temp += (float)(Mathf.Pow(1 - t, number - k - 1) * poss[k][j] * Mathf.Pow(t, k) * mi[k]);
                    }
                    result[i][j] = temp;
                }
            }

            return result;
        }

    }
}