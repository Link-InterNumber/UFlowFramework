
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PowerCellStudio
{
    public class MainCamera: MonoSingleton<MainCamera>
    {
        private Camera _cameraCom;
        private Volume _volume;
        
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
        }

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
    }
}