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

        private Vector3 _previousShakePosition;
        private Vector3 _posDelta;
        private Quaternion _previousShakeRotation;
        private bool _isCancelled;

        public ShakeHandle(ShakeRequest data)
        {
            _data = data;
            hashCode = _data.target.GetHashCode();
            time = 0;
            _previousShakeRotation = Quaternion.identity;
            _isCancelled = false;
        }

        public void Merge(ShakeHandle other)
        {
            if (other == null || other._data.target != _data.target) return;
            var newShakeRequest = new ShakeRequest
            {
                shakeType = other._data.shakeType | _data.shakeType,
                target = _data.target,
                duration = _data.duration + other._data.duration - time,
                frequency = Mathf.Max(_data.frequency, other._data.frequency),
                magnitude = Vector3.Max(_data.magnitude, other._data.magnitude),
                curve = other._data.curve != null ? other._data.curve : _data.curve,
                isUnscaleTime = other._data.isUnscaleTime,
                isCamera = _data.isCamera || other._data.isCamera
            };
            _data = newShakeRequest;
        }

        public void Cancel()
        {
            if (_isCancelled) return;
            _isCancelled = true;
            time = _data.duration + 1f;
            if (!_data.target) return;
            _data.target.localPosition = _data.target.localPosition - _previousShakePosition;
            _data.target.localRotation = _data.target.localRotation * Quaternion.Inverse(_previousShakeRotation);
        }

        public void Process(float dt)
        {
            if (isDone || _isCancelled) return;
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
                var shakeValue = new Vector3(x * _data.magnitude.x, y * _data.magnitude.y, z * _data.magnitude.z) * curvePosition;
                _posDelta = shakeValue - _previousShakePosition;
                _previousShakePosition = shakeValue;
                _data.target.localPosition = _data.target.localPosition + _posDelta;
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
                var rotDelta = shakeRotation * Quaternion.Inverse(_previousShakeRotation);
                _previousShakeRotation = shakeRotation;
                _data.target.localRotation = _data.target.localRotation * rotDelta;
            }
        }
    }
}