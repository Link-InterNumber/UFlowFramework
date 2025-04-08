using System;
using UnityEngine;

namespace PowerCellStudio
{
    public partial class AudioManager : MonoSingleton<AudioManager>
    {
        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
            InitEffectPlayer();
        }

        private void Update()
        {
            UpdateAudioRequest();
        }

        protected override void Deinit()
        {
            _musicPlayer?.DeInit();
            _musicPlayer = null;
            _ambiencePlayer?.DeInit();
            _ambiencePlayer = null;
            _dialogPlayer?.DeInit();
            _dialogPlayer = null;
            DeinitEffectPlayer();
            base.Deinit();
        }

        public float GetVolume(AudioSourceType type, bool isReal = false)
        {
            switch (type)
            {
                case AudioSourceType.Music:
                    return _musicPlayer.GetVolume(isReal);
                case AudioSourceType.Ambience:
                    return _ambiencePlayer.GetVolume(isReal);
                case AudioSourceType.UIEffect:
                    return isReal ? _UIEffectMaxVolume * _UIEffectVolume : _UIEffectVolume;
                case AudioSourceType.Effect3D:
                    return isReal ? _effectMaxVolume * _effectVolume : _effectVolume;
                case AudioSourceType.Dialog:
                    return _dialogPlayer.GetVolume(isReal);
                default:
                    return 0f;
            }
        }
        
        public void SetVolume(AudioSourceType type, float newValue, float transferTime = 0.3f)
        {
            var v = Mathf.Clamp01(newValue);
            switch (type)
            {
                case AudioSourceType.Music:
                    _musicPlayer.SetVolume(v, transferTime);
                    break;
                case AudioSourceType.Ambience:
                    _ambiencePlayer.SetVolume(v, transferTime);
                    break;
                case AudioSourceType.UIEffect:
                    _UIEffectVolume = v;
                    break;
                case AudioSourceType.Effect3D:
                    _effectVolume = v;
                    break;
                case AudioSourceType.Dialog:
                    _dialogPlayer.SetVolume(v, transferTime);
                    break;
                default:
                    break;
            }
        }
        
        public float GetMaxVolume(AudioSourceType type)
        {
            switch (type)
            {
                case AudioSourceType.Music:
                    return _musicPlayer.GetMaxVolume();
                case AudioSourceType.Ambience:
                    return _ambiencePlayer.GetMaxVolume();
                case AudioSourceType.UIEffect:
                    return _UIEffectMaxVolume;
                case AudioSourceType.Effect3D:
                    return _effectMaxVolume;
                case AudioSourceType.Dialog:
                    return _dialogPlayer.GetMaxVolume();
                default:
                    return 0f;
            }
        }
        
        public void SetMaxVolume(AudioSourceType type, float newValue)
        {
            var v = Mathf.Clamp01(newValue);
            switch (type)
            {
                case AudioSourceType.Music:
                    _musicPlayer.SetMaxVolume(v);
                    break;
                case AudioSourceType.Ambience:
                    _ambiencePlayer.SetMaxVolume(v);
                    break;
                case AudioSourceType.UIEffect:
                    _UIEffectMaxVolume = v;
                    break;
                case AudioSourceType.Effect3D:
                    _effectMaxVolume = v;
                    break;
                case AudioSourceType.Dialog:
                    _dialogPlayer.SetMaxVolume(v);
                    break;
                default:
                    break;
            }
        }
        
        public bool IsMute(AudioSourceType type)
        {
            switch (type)
            {
                case AudioSourceType.Music:
                    return _musicPlayer.IsMute;
                case AudioSourceType.Ambience:
                    return _ambiencePlayer.IsMute;
                case AudioSourceType.UIEffect:
                    return _muteUIEffect;
                case AudioSourceType.Effect3D:
                    return _muteEffect;
                case AudioSourceType.Dialog:
                    return _dialogPlayer.IsMute;
                default:
                    return true;
            }
        }
        
        public void Mute(AudioSourceType type, float transferDuration)
        {
            switch (type)
            {
                case AudioSourceType.Music:
                    _musicPlayer.Mute(transferDuration);
                    break;
                case AudioSourceType.Ambience:
                    _ambiencePlayer.Mute(transferDuration);
                    break;
                case AudioSourceType.UIEffect:
                    _muteUIEffect = true;
                    break;
                case AudioSourceType.Effect3D:
                    _muteEffect = true;
                    break;
                case AudioSourceType.Dialog:
                    _dialogPlayer.Mute(transferDuration);
                    break;
                default:
                    break;
            }
        }
        
        public void Unmute(AudioSourceType type, float transferDuration)
        {
            switch (type)
            {
                case AudioSourceType.Music:
                    _musicPlayer.Unmute(transferDuration);
                    break;
                case AudioSourceType.Ambience:
                    _ambiencePlayer.Unmute(transferDuration);
                    break;
                case AudioSourceType.UIEffect:
                    _muteUIEffect = false;
                    break;
                case AudioSourceType.Effect3D:
                    _muteEffect = false;
                    break;
                case AudioSourceType.Dialog:
                    _dialogPlayer.Unmute(transferDuration);
                    break;
                default:
                    break;
            }
        }
    }
}