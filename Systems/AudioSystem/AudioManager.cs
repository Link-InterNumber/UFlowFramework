using System;
using UnityEngine;

namespace PowerCellStudio
{
    /// <summary>
    /// 音频管理器类，负责不同类型音频的音量控制和静音处理。
    /// Audio manager class for handling volume control and muting of different audio types.
    /// </summary>
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

        private void CheckPlayer(AudioSourceType type)
        {
            switch (type)
            {
                case AudioSourceType.Music:
                    if (_musicPlayer != null) break;
                    _musicPlayer = MusicAudioSourcePlayer.Create(transform, "MusicPlayer");
                    break;
                case AudioSourceType.Ambience:
                    if (_ambiencePlayer != null) break;
                    _ambiencePlayer = MusicAudioSourcePlayer.Create(transform, "AmbiencePlayer");
                    break;
                case AudioSourceType.UIEffect:
                    break;
                case AudioSourceType.Effect3D:
                    break;
                case AudioSourceType.Dialog:
                    if (_dialogPlayer != null) break;
                    _dialogPlayer = DialogPlayer.Create(transform, "DialogPlayer");
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 获取指定类型音频的当前音量。
        /// Get the current volume of the specified audio source type.
        /// </summary>
        /// <param name="type">音频类型 / Type of audio source</param>
        /// <param name="isReal">是否获取真实音量 / Whether to get the real volume value</param>
        /// <returns>当前音量值 / Current volume value</returns>
        public float GetVolume(AudioSourceType type, bool isReal = false)
        {
            CheckPlayer(type);
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

        /// <summary>
        /// 设置指定类型音频的新音量。
        /// Set a new volume for the specified audio source type.
        /// </summary>
        /// <param name="type">音频类型 / Type of audio source</param>
        /// <param name="newValue">新的音量值 / New volume value</param>
        /// <param name="transferTime">过渡时间 / Transition duration</param>
        public void SetVolume(AudioSourceType type, float newValue, float transferTime = 0.3f)
        {
            CheckPlayer(type);
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

        /// <summary>
        /// 获取指定类型音频的最大音量。
        /// Get the maximum volume of the specified audio source type.
        /// </summary>
        /// <param name="type">音频类型 / Type of audio source</param>
        /// <returns>最大音量值 / Maximum volume value</returns>
        public float GetMaxVolume(AudioSourceType type)
        {
            CheckPlayer(type);
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

        /// <summary>
        /// 设置指定类型音频的最大音量。
        /// Set the maximum volume for the specified audio source type.
        /// </summary>
        /// <param name="type">音频类型 / Type of audio source</param>
        /// <param name="newValue">新的最大音量值 / New maximum volume value</param>
        public void SetMaxVolume(AudioSourceType type, float newValue)
        {
            CheckPlayer(type);
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

        /// <summary>
        /// 检查指定类型音频是否静音。
        /// Check whether the specified audio source type is muted.
        /// </summary>
        /// <param name="type">音频类型 / Type of audio source</param>
        /// <returns>是否静音 / Whether it is muted</returns>
        public bool IsMute(AudioSourceType type)
        {
            CheckPlayer(type);
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

        /// <summary>
        /// 静音指定类型音频。
        /// Mute the specified audio source type.
        /// </summary>
        /// <param name="type">音频类型 / Type of audio source</param>
        /// <param name="transferDuration">静音过渡时间 / Muting transition duration</param>
        public void Mute(AudioSourceType type, float transferDuration)
        {
            CheckPlayer(type);
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

        /// <summary>
        /// 取消静音指定类型音频。
        /// Unmute the specified audio source type.
        /// </summary>
        /// <param name="type">音频类型 / Type of audio source</param>
        /// <param name="transferDuration">取消静音过渡时间 / Unmuting transition duration</param>
        public void Unmute(AudioSourceType type, float transferDuration)
        {
            CheckPlayer(type);
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