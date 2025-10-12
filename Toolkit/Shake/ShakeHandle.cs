using UnityEngine;

namespace PowerCellStudio
{
    public class ShakeHandle: CustomYieldInstruction
    {
        private ShakeRequest _data;

        public int hashCode;

        private float time;

        public bool isDone => time >= _data.duration;

        public override bool keepWaiting => !isDone;

        public bool isUnscaleTime => _data.isUnscaleTime;

        public ShakeHandle(ShakeRequest data)
        {
            _data = data;
            hashCode = _data.target.GetHashCode();
            time = 0;
        }

        public void Cancel()
        {
            time = _data.duration + 1f;
            _data.target.localPosition = _data.origPos;
            _data.target.localRotation = _data.origRota;
        }

        public void Process(float dt)
        {
            time += dt;
            if (!_data.target)
            {
                time = _data.duration + 1f;
                return;
            }
            float curvePosition = _data.curve?.Evaluate(time / _data.duration) ?? 1f;
            if ((_data.shakeType & ShakeUtils.ShakeType.Position) != 0)
            {
                float x = Mathf.PerlinNoise(time * _data.frequency, 0) * 2 - 1; // 输出范围 [-1,1]
                float y = Mathf.PerlinNoise(time * _data.frequency, 1) * 2 - 1;
                float z = Mathf.PerlinNoise(time * _data.frequency, 2) * 2 - 1;
                var shakePosition = new Vector3(x * _data.magnitude.x, y * _data.magnitude.y, z * _data.magnitude.z) * curvePosition;
                _data.target.localPosition = _data.origPos + shakePosition;
            }
            if ((_data.shakeType & ShakeUtils.ShakeType.Rotation) != 0)
            {
                float x = Mathf.PerlinNoise(time * _data.frequency, 3) * 2 - 1; // 输出范围 [-1,1]
                float y = Mathf.PerlinNoise(time * _data.frequency, 4) * 2 - 1;
                float z = Mathf.PerlinNoise(time * _data.frequency, 5) * 2 - 1;
                var magnitude = _data.magnitude * 5;
                if (_data.isCamera)
                {
                    magnitude.z = Mathf.Max(magnitude.x, magnitude.y, magnitude.z);
                    magnitude.x = 0f;
                    magnitude.y = 0f;
                }
                Quaternion shakeRotation = Quaternion.Euler(
                    x * magnitude.x * curvePosition,
                    y * magnitude.y * curvePosition,
                    z * magnitude.z * curvePosition);
                _data.target.localRotation = _data.origRota * shakeRotation;
            }
        }
    }

    public struct ShakeRequest
    {
        public ShakeUtils.ShakeType shakeType;

        public Transform target;

        public float duration;

        public float frequency;

        public Vector3 magnitude;

        public AnimationCurve curve;

        public bool isUnscaleTime;

        public Vector3 origPos;

        public Quaternion origRota;

        public bool isCamera;

        public ShakeRequest(
        ShakeUtils.ShakeType shakeType,
        Transform target,
        float duration,
        float frequency,
        Vector3 magnitude,
        AnimationCurve curve,
        bool isUnscaleTime,
        bool isCamera)
        {
            this.shakeType = shakeType;
            this.target = target;
            this.duration = duration;
            this.frequency = frequency;
            this.magnitude = magnitude;
            this.curve = curve;
            this.isUnscaleTime = isUnscaleTime;
            this.isCamera = isCamera;

            origPos = target.position;
            origRota = target.localRotation;
      }
    }
}