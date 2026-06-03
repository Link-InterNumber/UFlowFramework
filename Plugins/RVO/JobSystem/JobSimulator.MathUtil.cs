using Unity.Mathematics;

namespace RVO.JobSystem
{
    public sealed partial class JobSimulator
    {
        private static class MathUtil
        {
            public static float AbsSq(float3 value)
            {
                return math.dot(value, value);
            }

            public static float LeftOf(float3 a, float3 b, float3 c)
            {
                return Det(a - c, b - a);
            }

            public static float Det(float3 lhs, float3 rhs)
            {
                return lhs.x * rhs.y - lhs.y * rhs.x;
            }

            public static float3 NormalizeSafe(float3 value)
            {
                var lengthSq = AbsSq(value);
                if (lengthSq <= RvoEpsilon * RvoEpsilon)
                {
                    return float3.zero;
                }

                return value * math.rsqrt(lengthSq);
            }

            public static float DistSqPointLineSegment(float3 segmentStart, float3 segmentEnd, float3 point)
            {
                var segment = segmentEnd - segmentStart;
                var segmentLengthSq = math.max(AbsSq(segment), RvoEpsilon);
                var t = math.dot(point - segmentStart, segment) / segmentLengthSq;

                if (t < 0.0f)
                {
                    return AbsSq(point - segmentStart);
                }

                if (t > 1.0f)
                {
                    return AbsSq(point - segmentEnd);
                }

                return AbsSq(point - (segmentStart + t * segment));
            }

            public static float DistSqSegmentSegment(float3 p1, float3 q1, float3 p2, float3 q2)
            {
                var d1 = q1 - p1;
                var d2 = q2 - p2;
                var r = p1 - p2;
                var a = AbsSq(d1);
                var e = AbsSq(d2);
                var f = math.dot(d2, r);

                float s;
                float t;

                if (a <= RvoEpsilon && e <= RvoEpsilon)
                {
                    return AbsSq(p1 - p2);
                }

                if (a <= RvoEpsilon)
                {
                    s = 0.0f;
                    t = math.clamp(f / math.max(e, RvoEpsilon), 0.0f, 1.0f);
                }
                else
                {
                    var c = math.dot(d1, r);
                    if (e <= RvoEpsilon)
                    {
                        t = 0.0f;
                        s = math.clamp(-c / math.max(a, RvoEpsilon), 0.0f, 1.0f);
                    }
                    else
                    {
                        var b = math.dot(d1, d2);
                        var denom = a * e - b * b;

                        if (math.abs(denom) > RvoEpsilon)
                        {
                            s = math.clamp((b * f - c * e) / denom, 0.0f, 1.0f);
                        }
                        else
                        {
                            s = 0.0f;
                        }

                        t = (b * s + f) / math.max(e, RvoEpsilon);
                        if (t < 0.0f)
                        {
                            t = 0.0f;
                            s = math.clamp(-c / math.max(a, RvoEpsilon), 0.0f, 1.0f);
                        }
                        else if (t > 1.0f)
                        {
                            t = 1.0f;
                            s = math.clamp((b - c) / math.max(a, RvoEpsilon), 0.0f, 1.0f);
                        }
                    }
                }

                var closest1 = p1 + d1 * s;
                var closest2 = p2 + d2 * t;
                return AbsSq(closest1 - closest2);
            }
        }
    }
}
