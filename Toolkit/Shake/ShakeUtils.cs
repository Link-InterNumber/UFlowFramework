using UnityEngine;

namespace PowerCellStudio
{
    public class ShakeUtils
    {
        [System.Flags]
        public enum ShakeType
        {
            None = 0,
            Position = 1 << 0, // 1
            Rotation = 1 << 1, // 2
        }

        public enum ShakePreset
        {
            Small,
            Medium,
            Large
        }

        private static ShakeProcesser _processer;

        public static ShakeProcesser GetShakeProcesser()
        {
            if (!_processer)
            {
                var obj = new GameObject("ShakeProcesser");
                _processer = obj.AddComponent<ShakeProcesser>();
            }
            return _processer;
        }

        public static ShakeHandle Shake(ShakeType shakeType, Transform target, float duration, float frequency, Vector3 magnitude,
             bool isUnscaleTime = true, AnimationCurve curve = null)
        {
            if (!target) return null;
            var isCamera = target.GetComponent<Camera>() != null;
            var request = new ShakeRequest(shakeType, target, duration, frequency, magnitude, curve, isUnscaleTime, isCamera);
            var handle = new ShakeHandle(request);
            GetShakeProcesser().PushHandle(handle);
            return handle;
        }

        public static ShakeHandle ShakeByPreset(Transform target, ShakePreset preset, float scale = 1f, ShakeType shakeType = ShakeType.Position | ShakeType.Rotation, float duration = 0.5f, bool isUnscaleTime = true, AnimationCurve curve = null)
        {
            Vector3 magnitude;
            float frequency;
            switch (preset)
            {
                case ShakePreset.Small:
                    magnitude = new Vector3(0.05f, 0.05f, 0.05f) * scale;
                    frequency = 20f;
                    break;
                case ShakePreset.Medium:
                    magnitude = new Vector3(0.15f, 0.15f, 0.15f) * scale;
                    frequency = 30f;
                    break;
                case ShakePreset.Large:
                    magnitude = new Vector3(0.3f, 0.3f, 0.3f) * scale;
                    frequency = 40f;
                    break;
                default:
                    magnitude = Vector3.zero;
                    frequency = 0f;
                    break;
            }
            return Shake(shakeType, target, duration, frequency, magnitude, isUnscaleTime, curve);
        }

        public static ShakeHandle ShakeCamera(Camera camera, float duration, float frequency, Vector2 magnitude,
            ShakeType shakeType = ShakeType.Position | ShakeType.Rotation, bool isUnscaleTime = true, AnimationCurve curve = null)
        {
            if (camera == null) return null;
            return Shake(shakeType, camera.transform, duration, frequency, (Vector3)magnitude, isUnscaleTime, curve);
        }

        public static ShakeHandle ShakeCameraByPreset(Camera camera, ShakePreset preset, float scale = 1f, ShakeType shakeType = ShakeType.Position | ShakeType.Rotation, float duration = 0.5f, bool isUnscaleTime = true, AnimationCurve curve = null)
        {
            Vector3 magnitude;
            float frequency;
            switch (preset)
            {
                case ShakePreset.Small:
                    magnitude = new Vector3(0.05f, 0.05f, 0f) * scale;
                    frequency = 20f;
                    break;
                case ShakePreset.Medium:
                    magnitude = new Vector3(0.15f, 0.15f, 0f) * scale;
                    frequency = 30f;
                    break;
                case ShakePreset.Large:
                    magnitude = new Vector3(0.3f, 0.3f, 0f) * scale;
                    frequency = 40f;
                    break;
                default:
                    magnitude = Vector3.zero;
                    frequency = 0f;
                    break;
            }
            return ShakeCamera(camera, duration, frequency, magnitude, shakeType, isUnscaleTime, curve);
        }
    }
}