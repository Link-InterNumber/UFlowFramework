using System;
using UnityEngine;

namespace PowerCellStudio
{
    public enum EaseType
    {
        Linear,
        InQuad,
        OutQuad,
        InOutQuad,
        InCubic,
        OutCubic,
        InOutCubic,
        InQuart,
        OutQuart,
        InOutQuart,
        InQuint,
        OutQuint,
        InOutQuint,
        InSine,
        OutSine,
        InOutSine,
        InEnormalizedTimepo,
        OutEnormalizedTimepo,
        InOutEnormalizedTimepo,
        InCirc,
        OutCirc,
        InOutCirc,
        InBack,
        OutBack,
        InOutBack,
        InElastic,
        OutElastic,
        InOutElastic,
        InBounce,
        OutBounce,
        InOutBounce,
    }
    
    public class Ease
    {
        private const float PI = Mathf.PI;
        private const float C1 = 1.70158f;
        private const float C2 = C1 * 1.525f;
        private const float C3 = C1 + 1f;
        private const float C4 = (2f * PI) / 3f;
        private const float C5 = (2f * PI) / 4.5f;

        public static float GetEase(EaseType type, float normalizedTime)
        {
            switch (type)
            {
                case EaseType.Linear:
                    return Linear(normalizedTime);
                case EaseType.InQuad:
                    return EaseInQuad(normalizedTime);
                case EaseType.OutQuad:
                    return EaseOutQuad(normalizedTime);
                case EaseType.InOutQuad:
                    return EaseInOutQuad(normalizedTime);
                case EaseType.InCubic:
                    return EaseInCubic(normalizedTime);
                case EaseType.OutCubic:
                    return EaseOutCubic(normalizedTime);
                case EaseType.InOutCubic:
                    return EaseInOutCubic(normalizedTime);
                case EaseType.InQuart:
                    return EaseInQuart(normalizedTime);
                case EaseType.OutQuart:
                    return EaseOutQuart(normalizedTime);
                case EaseType.InOutQuart:
                    return EaseInOutQuart(normalizedTime);
                case EaseType.InQuint:
                    return EaseInQuint(normalizedTime);
                case EaseType.OutQuint:
                    return EaseOutQuint(normalizedTime);
                case EaseType.InOutQuint:
                    return EaseInOutQuint(normalizedTime);
                case EaseType.InSine:
                    return EaseInSine(normalizedTime);
                case EaseType.OutSine:
                    return EaseOutSine(normalizedTime);
                case EaseType.InOutSine:
                    return EaseInOutSine(normalizedTime);
                case EaseType.InEnormalizedTimepo:
                    return EaseInEnormalizedTimepo(normalizedTime);
                case EaseType.OutEnormalizedTimepo:
                    return EaseOutEnormalizedTimepo(normalizedTime);
                case EaseType.InOutEnormalizedTimepo:
                    return EaseInOutEnormalizedTimepo(normalizedTime);
                case EaseType.InCirc:
                    return EaseInCirc(normalizedTime);
                case EaseType.OutCirc:
                    return EaseOutCirc(normalizedTime);
                case EaseType.InOutCirc:
                    return EaseInOutCirc(normalizedTime);
                case EaseType.InBack:
                    return EaseInBack(normalizedTime);
                case EaseType.OutBack:
                    return EaseOutBack(normalizedTime);
                case EaseType.InOutBack:
                    return EaseInOutBack(normalizedTime);
                case EaseType.InElastic:
                    return EaseInElastic(normalizedTime);
                case EaseType.OutElastic:
                    return EaseOutElastic(normalizedTime);
                case EaseType.InOutElastic:
                    return EaseInOutElastic(normalizedTime);
                case EaseType.InBounce:
                    return EaseInBounce(normalizedTime);
                case EaseType.OutBounce:
                    return EaseOutBounce(normalizedTime);
                case EaseType.InOutBounce:
                    return EaseInOutBounce(normalizedTime);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        
        public static Func<float, float> GetEase(EaseType type)
        {
            switch (type)
            {
                case EaseType.Linear:
                    return Linear;
                case EaseType.InQuad:
                    return EaseInQuad;
                case EaseType.OutQuad:
                    return EaseOutQuad;
                case EaseType.InOutQuad:
                    return EaseInOutQuad;
                case EaseType.InCubic:
                    return EaseInCubic;
                case EaseType.OutCubic:
                    return EaseOutCubic;
                case EaseType.InOutCubic:
                    return EaseInOutCubic;
                case EaseType.InQuart:
                    return EaseInQuart;
                case EaseType.OutQuart:
                    return EaseOutQuart;
                case EaseType.InOutQuart:
                    return EaseInOutQuart;
                case EaseType.InQuint:
                    return EaseInQuint;
                case EaseType.OutQuint:
                    return EaseOutQuint;
                case EaseType.InOutQuint:
                    return EaseInOutQuint;
                case EaseType.InSine:
                    return EaseInSine;
                case EaseType.OutSine:
                    return EaseOutSine;
                case EaseType.InOutSine:
                    return EaseInOutSine;
                case EaseType.InEnormalizedTimepo:
                    return EaseInEnormalizedTimepo;
                case EaseType.OutEnormalizedTimepo:
                    return EaseOutEnormalizedTimepo;
                case EaseType.InOutEnormalizedTimepo:
                    return EaseInOutEnormalizedTimepo;
                case EaseType.InCirc:
                    return EaseInCirc;
                case EaseType.OutCirc:
                    return EaseOutCirc;
                case EaseType.InOutCirc:
                    return EaseInOutCirc;
                case EaseType.InBack:
                    return EaseInBack;
                case EaseType.OutBack:
                    return EaseOutBack;
                case EaseType.InOutBack:
                    return EaseInOutBack;
                case EaseType.InElastic:
                    return EaseInElastic;
                case EaseType.OutElastic:
                    return EaseOutElastic;
                case EaseType.InOutElastic:
                    return EaseInOutElastic;
                case EaseType.InBounce:
                    return EaseInBounce;
                case EaseType.OutBounce:
                    return EaseOutBounce;
                case EaseType.InOutBounce:
                    return EaseInOutBounce;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static float BounceOut(float normalizedTime)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (normalizedTime < 1f / d1)
            {
                return n1 * normalizedTime * normalizedTime;
            }

            if (normalizedTime < 2f / d1)
            {
                return n1 * (normalizedTime -= 1.5f / d1) * normalizedTime + 0.75f;
            }

            if (normalizedTime < 2.5f / d1)
            {
                return n1 * (normalizedTime -= 2.25f / d1) * normalizedTime + 0.9375f;
            }

            return n1 * (normalizedTime -= 2.625f / d1) * normalizedTime + 0.984375f;
        }

        private static float Linear(float normalizedTime)
        {
            return normalizedTime;
        }

        private static float EaseInQuad(float normalizedTime)
        {
            return normalizedTime * normalizedTime;
        }

        private static float EaseOutQuad(float normalizedTime)
        {
            return 1f - (1f - normalizedTime) * (1f - normalizedTime);
        }

        private static float EaseInOutQuad(float normalizedTime)
        {
            return normalizedTime < 0.5f
                ? 2f * normalizedTime * normalizedTime
                : 1f - Mathf.Pow(-2f * normalizedTime + 2, 2) * 0.5f;
        }

        private static float EaseInCubic(float normalizedTime)
        {
            return normalizedTime * normalizedTime * normalizedTime;
        }

        private static float EaseOutCubic(float normalizedTime)
        {
            return 1f - Mathf.Pow(1f - normalizedTime, 3f);
        }

        private static float EaseInOutCubic(float normalizedTime)
        {
            return normalizedTime < 0.5f
                ? 4f * normalizedTime * normalizedTime * normalizedTime
                : 1f - Mathf.Pow(-2 * normalizedTime + 2, 3) * 0.5f;
        }

        private static float EaseInQuart(float normalizedTime)
        {
            return normalizedTime * normalizedTime * normalizedTime * normalizedTime;
        }

        private static float EaseOutQuart(float normalizedTime)
        {
            return 1f - Mathf.Pow(1f - normalizedTime, 4f);
        }

        private static float EaseInOutQuart(float normalizedTime)
        {
            return normalizedTime < 0.5f
                ? 8f * normalizedTime * normalizedTime * normalizedTime * normalizedTime
                : 1f - Mathf.Pow(-2f * normalizedTime + 2f, 4f) * 0.5f;
        }

        private static float EaseInQuint(float normalizedTime)
        {
            return normalizedTime * normalizedTime * normalizedTime * normalizedTime * normalizedTime;
        }

        private static float EaseOutQuint(float normalizedTime)
        {
            return 1f - Mathf.Pow(1f - normalizedTime, 5f);
        }

        private static float EaseInOutQuint(float normalizedTime)
        {
            return normalizedTime < 0.5f
                ? 16f * normalizedTime * normalizedTime * normalizedTime * normalizedTime * normalizedTime
                : 1f - Mathf.Pow(-2f * normalizedTime + 2f, 5f) * 0.5f;
        }

        private static float EaseInSine(float normalizedTime)
        {
            return 1 - Mathf.Cos((normalizedTime * PI) * 0.5f);
        }

        private static float EaseOutSine(float normalizedTime)
        {
            return Mathf.Sin((normalizedTime * PI) * 0.5f);
        }

        private static float EaseInOutSine(float normalizedTime)
        {
            return -(Mathf.Cos(PI * normalizedTime) - 1f) * 0.5f;
        }

        private static float EaseInEnormalizedTimepo(float normalizedTime)
        {
            return normalizedTime == 0f ? 0f : Mathf.Pow(2f, 10f * normalizedTime - 10f);
        }

        private static float EaseOutEnormalizedTimepo(float normalizedTime)
        {
            return normalizedTime >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * normalizedTime);
        }

        private static float EaseInOutEnormalizedTimepo(float normalizedTime)
        {
            return normalizedTime == 0f
                ? 0f
                : normalizedTime >= 1f
                    ? 1f
                    : normalizedTime < 0.5f
                        ? Mathf.Pow(2f, 20f * normalizedTime - 10f) * 0.5f
                        : (2f - Mathf.Pow(2f, -20f * normalizedTime + 10f)) * 0.5f;
        }

        private static float EaseInCirc(float normalizedTime)
        {
            return 1f - Mathf.Sqrt(1f - Mathf.Pow(normalizedTime, 2f));
        }

        private static float EaseOutCirc(float normalizedTime)
        {
            return Mathf.Sqrt(1f - Mathf.Pow(normalizedTime - 1f, 2f));
        }

        private static float EaseInOutCirc(float normalizedTime)
        {
            return normalizedTime < 0.5f
                ? (1f - Mathf.Sqrt(1f - Mathf.Pow(2f * normalizedTime, 2))) * 0.5f
                : (Mathf.Sqrt(1f - Mathf.Pow(-2f * normalizedTime + 2, 2)) + 1) * 0.5f;
        }

        private static float EaseInBack(float normalizedTime)
        {
            return C3 * normalizedTime * normalizedTime * normalizedTime - C1 * normalizedTime * normalizedTime;
        }

        private static float EaseOutBack(float normalizedTime)
        {
            return 1f + C3 * Mathf.Pow(normalizedTime - 1f, 3f) + C1 * (normalizedTime - 1f) * (normalizedTime - 1f);
        }

        private static float EaseInOutBack(float normalizedTime)
        {
            return normalizedTime < 0.5f
                ? (Mathf.Pow(2f * normalizedTime, 2f) * ((C2 + 1f) * 2f * normalizedTime - C2)) * 0.5f
                : (Mathf.Pow(2f * normalizedTime - 2f, 2f) * ((C2 + 1f) * (normalizedTime * 2f - 2f) + C2) + 2f) * 0.5f;
        }

        private static float EaseInElastic(float normalizedTime)
        {
            return normalizedTime == 0f
                ? 0f
                : normalizedTime >= 1f
                    ? 1f
                    : -Mathf.Pow(2f, 10f * normalizedTime - 10f) * Mathf.Sin((normalizedTime * 10f - 10.75f) * C4);
        }

        private static float EaseOutElastic(float normalizedTime)
        {
            return normalizedTime == 0f
                ? 0f
                : normalizedTime >= 1f
                    ? 1f
                    : Mathf.Pow(2f, -10f * normalizedTime) * Mathf.Sin((normalizedTime * 10f - 0.75f) * C4) + 1f;
        }

        private static float EaseInOutElastic(float normalizedTime)
        {
            return normalizedTime == 0f
                ? 0f
                : normalizedTime >= 1f
                    ? 1f
                    : normalizedTime < 0.5f
                        ? -(Mathf.Pow(2f, 20f * normalizedTime - 10f) * Mathf.Sin((20f * normalizedTime - 11.125f) * C5)) *0.5f
                        : (Mathf.Pow(2f, -20f * normalizedTime + 10f) * Mathf.Sin((20f * normalizedTime - 11.125f) * C5)) *0.5f + 1f;
        }

        private static float EaseInBounce(float normalizedTime)
        {
            return 1f - BounceOut(1f - normalizedTime);
        }

        private static float EaseOutBounce(float normalizedTime)
        {
            return BounceOut(normalizedTime);
        }

        private static float EaseInOutBounce(float normalizedTime)
        {
            return normalizedTime < 0.5f
                ? (1f - BounceOut(1f - 2f * normalizedTime)) * 0.5f
                : (1f + BounceOut(2f * normalizedTime - 1)) * 0.5f;
        }
    }
}