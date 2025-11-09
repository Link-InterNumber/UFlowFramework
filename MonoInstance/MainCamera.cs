
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PowerCellStudio
{
    [DonotInitModuleAutoly]
    public class MainCamera: MonoSingleton<MainCamera>
    {
        private Camera _cameraCom;
        private Transform _cameraRoot;
        private Volume _volume;

        private Quaternion _offsetRotation;
        private Quaternion _targetRotation;
        
        public Camera CameraCom
        {
            get
            {
                if (!_cameraCom)
                    _cameraCom = transform.GetComponent<Camera>();
                return _cameraCom;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _cameraCom = transform.GetComponent<Camera>();
            _volume = transform.GetComponent<Volume>();
            _cameraRoot = new GameObject("CameraRoot").transform;
            _cameraRoot.SetParent(_cameraCom.transform.parent); 
            _cameraRoot.localPosition = _cameraCom.transform.localPosition;
            _cameraRoot.localRotation = Quaternion.identity;
            _cameraCom.transform.SetParent(_cameraRoot);
            _cameraCom.transform.localPosition = Vector3.zero;
            _rootOriginPos = _cameraRoot.localPosition;
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            CheckShake(dt);
            CheckTween(dt);
        }

#region Move

        private Vector3 _offsetPosition = Vector3.zero;
        private Vector3 _targetPosition;
        private bool _inMoveTween = false;

        private float _tweenSpeed;
        public float tweenSpeed 
        {
            get => _tweenSpeed;
            set
            {
                _tweenSpeed = Mathf.Max(0f, value);
            }
        }

        public Vector3 offsetPosition 
        {
            get => _offsetPosition;
            set
            {
                _offsetPosition = value;
            }
        }

        public float x => _cameraRoot.position.x;
        public float y => _cameraRoot.position.y;
        public float z => _cameraRoot.position.z;

        private void CheckTween(float dt)
        {
            if (!_inMoveTween) return;
            if(_targetPosition.ManhattanDistance(_cameraRoot.position) < 0.1f)
            {
                _cameraRoot.position = _targetPosition + _offsetPosition;
                _inMoveTween = false;
                return;
            }
            _cameraRoot.position = Vector3.Lerp(_cameraRoot.localPosition, _targetPosition + _offsetPosition, dt * _tweenSpeed);
        }

        public void TweenTargetPos(Vector3 targetPos)
        {
            _targetPosition = targetPos;
            if (_tweenSpeed <= 0)
            {
                _cameraRoot.position = _targetPosition;
                _inMoveTween = false;
            }
            else
            {
                _inMoveTween = true;
            }
        }

#endregion

#region PostProcessing

        public void EnablePostProcessing<T>(bool enable) where T : VolumeComponent
        {
            if (!_volume) return;
            _volume.enabled = true;
            if (!_volume.profile.TryGet<T>(out var effect)) return;
            effect.active = enable;
            // 检查是否有后处理效果开启
            var hasEffect = false;
            foreach (var component in _volume.profile.components)
            {
                if (component.active)
                {
                    hasEffect = true;
                    break;
                }
            }
            _cameraCom.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = hasEffect;
        }
        
        public void EnableGaussBlur(bool enable)
        {
            EnablePostProcessing<GaussBlur>(enable);
        }

#endregion

#region Shake

        [System.Flags]
        public enum ShakeType
        {
            None = 0,
            Position = 1 << 0, // 位移震动
            Rotation = 2 << 0,  // 旋转震动
        }

        // private class ShakeQuest
        // {
        //     public float shakeDuration = 1f; // 震动持续时间
        //     public ShakeType currentShakeType; // 当前震动类型
        //     public float shakeAmplitude = 0f; // 当前震动幅度（控制值）
        // }

        private Vector3 _rootOriginPos;
        // public float shakeDuration = 1f; // 震动持续时间
        public float shakeFrequency = 25f; // Perlin Noise 的频率
        public float shakeScale = 1f;
        public float rotationShakeAmplitude = 15f;
        private float _shakeAmplitude = 0f; // 当前震动幅度（控制值）
        private float _shakeTime = 0f; // 当前震动时间
        private ShakeType _currentShakeType; // 当前震动类型

        private void CheckShake(float dt)
        {
            if (_shakeTime <= 0) return;
            // 根据震动类型执行不同的震动逻辑
            if ((_currentShakeType & ShakeType.Position) != 0)
            {
                ApplyPositionShake(dt);
            }
            if ((_currentShakeType & ShakeType.Rotation) != 0)
            {
                ApplyRotationShake(dt);
            }

            // 减少震动时间
            _shakeTime -= Time.deltaTime;

            // 如果震动结束，重置摄像机位置和旋转
            if (_shakeTime <= 0)
            {
                _cameraCom.transform.localPosition = _rootOriginPos;
                _cameraCom.transform.localRotation = Quaternion.identity;
                _currentShakeType = ShakeType.None;
            }
        }

        private float _shakeX, _shakeY;
        
        /// <summary>
        /// 应用位移震动
        /// </summary>
        private void ApplyPositionShake(float dt)
        {
            _shakeX += dt * shakeFrequency;
            if (_shakeX > 1080f || _shakeX < -1080f) _shakeX = 0f;
            _shakeY += dt * shakeFrequency;
            if (_shakeY > 1080f || _shakeY < -1080f) _shakeY = 0f;
            // 使用 Perlin Noise 生成随机偏移
            float noiseX = Mathf.PerlinNoise(_shakeX, 0f) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(0f, _shakeY) * 2f - 1f;

            // 根据控制值的平方计算实际震动幅度
            float amplitude = _shakeAmplitude * _shakeAmplitude * shakeScale;

            // 应用震动偏移
            Vector3 shakeOffset = new Vector3(noiseX * amplitude, noiseY * amplitude, _rootOriginPos.z);
            _cameraCom.transform.localPosition = shakeOffset;
        }


        private float _rotationXY;
        
        /// <summary>
        /// 应用旋转震动
        /// </summary>
        private void ApplyRotationShake(float dt)
        {
            _rotationXY += dt * shakeFrequency;
            if (_rotationXY > 1080f) _rotationXY = 0f;
            // 使用 Perlin Noise 生成随机旋转
            float noiseY = Mathf.PerlinNoise(_rotationXY, _rotationXY) * 2f - 1f;
            // float noiseY = Mathf.PerlinNoise(0f, dt * shakeFrequency) * 2f - 1f;

            // 根据控制值的平方计算实际震动幅度
            float amplitude = _shakeAmplitude * _shakeAmplitude * shakeScale;

            // 应用震动旋转
            Vector3 shakeRotation = new Vector3(0f, 0f, noiseY) * (amplitude * rotationShakeAmplitude); // 旋转幅度可以适当放大
            _cameraCom.transform.localRotation = Quaternion.Euler(shakeRotation);
        }

        /// <summary>
        /// 开始屏幕震动
        /// </summary>
        /// <param name="amplitude">震动幅度控制值（0 到 1）</param>
        /// <param name="duration">震动持续时间</param>
        /// <param name="shakeType">震动类型（位移或旋转）</param>
        public void StartShake(float amplitude, float duration, ShakeType shakeType)
        {
            _shakeAmplitude = Mathf.Clamp01(amplitude); // 确保控制值在 [0, 1] 范围内
            // shakeDuration = duration;
            _shakeTime = duration;
            _currentShakeType = shakeType;
        }

#endregion

#region Tool

        /// <summary>
        /// 获取摄像机视野内的世界包围盒（AABB）。
        /// Get the world axis-aligned bounding box (AABB) of the camera's view.
        /// </summary>
        /// <param name="camera">目标摄像机 / Target camera.</param>
        /// <param name="z">世界空间下的深度 / Z value in world space.</param>
        /// <returns>包围盒Bounds / The bounds in world space.</returns>
        public Bounds GetViewBounds(float z = 0f)
        {
            Vector3 min = _cameraCom.ViewportToWorldPoint(new Vector3(0, 0, z));
            Vector3 max = _cameraCom.ViewportToWorldPoint(new Vector3(1, 1, z));
            return new Bounds((min + max) * 0.5f, max - min);
        }

        /// <summary>
        /// 判断某点是否在摄像机视野内。
        /// Check if a point is within the camera's view.
        /// </summary>
        public bool IsInView(Vector3 worldPos)
        {
            Vector3 viewportPoint = _cameraCom.WorldToViewportPoint(worldPos);
            return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
                viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
                viewportPoint.z > 0;
        }

        /// <summary>
        /// 获取摄像机屏幕中心的世界坐标。
        /// Get the world position at the center of the camera's screen.
        /// </summary>
        /// <param name="camera">目标摄像机 / Target camera.</param>
        /// <param name="z">世界空间下的深度 / Z value in world space.</param>
        /// <returns>世界坐标 / World position.</returns>
        public Vector3 GetScreenCenterWorldPosition(float z = 0f)
        {
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, z);
            return _cameraCom.ScreenToWorldPoint(screenCenter);
        }

        /// <summary>
        /// 将摄像机对准目标点。
        /// Make the camera look at a target position.
        /// </summary>
        /// <param name="camera">目标摄像机 / Target camera.</param>
        /// <param name="target">目标点 / Target position.</param>
        public void LookAt(Vector3 target)
        {
            _cameraCom.transform.LookAt(target);
        }

#endregion
    }
}