using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PowerCellStudio
{
    public static class Bezier
    {
        
        /// <summary>
        /// 操作transform进行贝塞尔曲线移动
        /// Move a transform along a Bezier curve.
        /// </summary>
        /// <param name="transform">控制节点 - The transform to be moved.</param>
        /// <param name="duration">动画时长 - Duration of the movement.</param>
        /// <param name="endPos">曲线的终点 - The end of Bezier curve</param>
        /// <param name="armLengthScale">控制点和起点距离 = 起点到终点的距离 * armLengthScale -  Control point and starting point distance = Distance from starting point to endpoint * arm length scale</param>
        /// <param name="onComplete">完成的回调 - call on transform tween to end pos</param>
        /// <returns></returns>
        public static AsyncHandlerBase BezierMove(Transform transform, float duration, Vector2 endPos, float armLengthScale = 0.5f, Action<Transform> onComplete = null)
        {
            if (!transform) return null;
            duration = Mathf.Max(0f, duration);
            var distance = Vector3.Distance(endPos, transform.position);
            var armLength = armLengthScale * distance;
            var ctrlPos = transform.position + new Vector3(Random.Range(-armLength, armLength), Random.Range(-armLength, armLength), 0);
            var points = new List<Vector3> { transform.position, ctrlPos, endPos };
            return AsyncManager.Run(BezierMoveHandler(transform, duration, points, true, false, onComplete));
        }
        
        /// <summary>
        /// 操作transform进行贝塞尔曲线移动
        /// Move a transform along a Bezier curve.
        /// </summary>
        /// <param name="transform">控制节点 - The transform to be moved.</param>
        /// <param name="duration">动画时长 - Duration of the movement.</param>
        /// <param name="points">起/终点和控制点，第一个为起点，最后一个为终点 - Start/end points and control points, first is start, last is end.</param>
        /// <param name="unscaleTime">不受时间缩放影响，默认false - Whether time scaling affects the animation.</param>
        /// <param name="isLocalPos">使用本地位置，默认false - Whether to use local position.</param>
        /// <param name="onComplete">完成的回调 - call on transform tween to end pos</param>
        /// <returns>协程，由ApplicationManager启动 - Coroutine launched by ApplicationManager.</returns>
        public static AsyncHandlerBase BezierMove(Transform transform, float duration, IList<Vector3> points, bool unscaleTime = false, bool isLocalPos = false, Action<Transform> onComplete = null)
        {
            if (!transform || points == null || points.Count < 2) return null;
            duration = Mathf.Max(0f, duration);
            return AsyncManager.Run(BezierMoveHandler(transform, duration, points, unscaleTime, isLocalPos, onComplete));
        }

        private static IEnumerator BezierMoveHandler(Transform transform, float duration, IList<Vector3> points, bool unscaleTime = false, bool isLocalPos = false, Action<Transform> onComplete = null)
        {
            var time = 0f;
            while (time < duration)
            {
                var t = time / duration;
                var pos = CalcBezierPoint(t, points);
                if (isLocalPos)
                {
                    transform.localPosition = pos;
                }
                else
                {
                    transform.position = pos;
                }
                time += unscaleTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
            var lastPos = points[points.Count - 1];
            if (isLocalPos)
            {
                transform.localPosition = lastPos;
            }
            else
            {
                transform.position = lastPos;
            }
            onComplete?.Invoke(transform);
        }

        /// <summary>
        /// 根据T值，计算二次贝塞尔曲线上面相对应的点
        /// Compute a point on a quadratic Bezier curve based on the T value.
        /// </summary>
        /// <param name="t">T值（0-1） - T value (0-1).</param>
        /// <param name="p0">起始点 - Start point.</param>
        /// <param name="p1">控制点 - Control point.</param>
        /// <param name="p2">目标点 - End point.</param>
        /// <returns>根据T值计算出来的贝赛尔曲线点 - Point on Bezier curve.</returns>
        public static Vector3 CalcBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
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
        /// Compute a point on a cubic Bezier curve based on the T value.
        /// </summary>
        /// <param name="t">T值（0-1） - T value (0-1).</param>
        /// <param name="p0">起始点 - Start point.</param>
        /// <param name="p1">控制点1 - First control point.</param>
        /// <param name="p2">控制点2 - Second control point.</param>
        /// <param name="p3">目标点 - End point.</param>
        /// <returns>计算得到的贝塞尔曲线点 - Point on Bezier curve.</returns>
        public static Vector3 CalcBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
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
        /// 计算任意数量控制点的贝塞尔曲线事件时
        /// Compute a point on a Bezier curve with arbitrary control points.
        /// </summary>
        /// <param name="t">T值（0-1） - T value (0-1).</param>
        /// <param name="points">控制点列表 - List of control points.</param>
        /// <returns>计算得到的贝塞尔曲线点 - Point on Bezier curve.</returns>
        public static Vector3 CalcBezierPoint(float t, IList<Vector3> points)
        {
            if (points == null || points.Count == 0)
            {
                LinkLog.LogError("控制点列表不能为空");
                return Vector3.zero;
            }

            if (t <= 0) return points[0];
            if (t >= 1) return points[points.Count - 1];

            if (points.Count == 1)
            {
                LinkLog.LogError("控制点数量需要大于1");
                return points[0];
            }
            if (points.Count == 2)
            {
                return Vector3.Lerp(points[0], points[1], t);
            }
            else if (points.Count == 3)
            {
                return CalcBezierPoint(t, points[0], points[1], points[2]);
            }
            else if (points.Count == 4)
            {
                return CalcBezierPoint(t, points[0], points[1], points[2], points[3]);
            }
            
            int n = points.Count;
            Vector3[] tempPoints = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                tempPoints[i] = points[i];
            }

            for (int j = 1; j < n; j++)
            {
                for (int i = 0; i < n - j; i++)
                {
                    tempPoints[i] = (1 - t) * tempPoints[i] + t * tempPoints[i + 1];
                }
            }

            return tempPoints[0];
        }

        /// <summary>
        /// 获取存储贝塞尔曲线采样点
        /// Sample a Bezier curve and get stored sample points.
        /// </summary>
        /// <param name="startPoint">起始点 - Start point.</param>
        /// <param name="controlPoint">控制点 - Control point.</param>
        /// <param name="endPoint">目标点 - End point.</param>
        /// <param name="segmentNum">采样点的数量(最小2，即起点和终点) - Number of segments for sampling (minimum 2).</param>
        /// <returns>存储贝塞尔曲线点的数组 - Array of sampled Bezier curve points.</returns>
        public static Vector3[] SampleBezierPath(Vector3 startPoint, Vector3 controlPoint, Vector3 endPoint, int segmentNum)
        {
            segmentNum = Mathf.Max(2, segmentNum);
            Vector3[] path = new Vector3[segmentNum];
            for (int i = 0; i < segmentNum; i++) // Fix out-of-range issue
            {
                float t = (float)i / (float)(segmentNum - 1);
                Vector3 point = CalcBezierPoint(t, startPoint, controlPoint, endPoint);
                path[i] = point;
            }
            return path;
        }

        private static int Precision(IList<Vector3> points, float uniDistance)
        {
            var result = 0;
            for (int i = 0; i < points.Count - 1; i++)
            {
                var delta = points[i] - points[i + 1];
                result += (int)(delta.magnitude / uniDistance);
            }

            return result;
        }

        /// <summary>
        /// 计算Bezier曲线上的点
        /// Compute points on the Bezier curve.
        /// </summary>
        /// <param name="poss">贝塞尔曲线控制点坐标 - Control points positions.</param>
        /// <param name="uniDistance">采样精度 - Sampling accuracy</param>
        /// <returns>该条贝塞尔曲线上的点（三维坐标） - Points on Bezier curve (3D).</returns>
        public static Vector3[] Path(IList<Vector3> poss, float uniDistance = 0.08f)
        {
            var precision = Precision(poss, uniDistance);
            var result = new Vector3[precision];
            Path(poss, ref result);
            return result;
        }

        /// <summary>
        /// 计算Bezier曲线上的点
        /// Compute points on the Bezier curve.
        /// </summary>
        /// <param name="poss">贝塞尔曲线起/终点、控制点坐标 - Control points positions.</param>
        /// <param name="result">该条贝塞尔曲线上的点（三维坐标）数组，数组长度用于控制精度 
        /// - An array of points (3D coordinates) on the Bezier curve, and the length of the array is used to control the accuracy.</param>
        public static void Path(IList<Vector3> poss, ref Vector3[] result)
        {
            if (poss == null || result == null) return;
            if (poss.Count < 2)
            {
                LinkLog.LogError("控制点数量需要大于1");
                return;
            }
            if (result.Length < 2)
            {
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = poss[i];
                }
                return;
            }
            var loopNumber = result.Length;
            for (int i = 0; i < loopNumber; i++)
            {
                var t = i / (loopNumber - 1f);
                result[i] = CalcBezierPoint(t, poss);
            }
        }
    }
}