using System;

namespace PowerCellStudio
{
    /// <summary>
    /// 音频管理器类，负责不同类型音频的音量控制和静音处理。
    /// Audio manager class for handling volume control and muting of different audio types.
    /// </summary>
    public partial class AudioManager : MonoSingleton<AudioManager>
    {
        private IAssetLoader _assetLoader;
        
        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
            _assetLoader = AssetUtils.SpawnLoader("AudioManager");
            BuildPipeLine();
        }

        protected override void Deinit()
        {
            _masterPipeline?.Dispose();
            AssetUtils.DeSpawnLoader(_assetLoader);
            base.Deinit();
        }

        private void Update()
        {
            for (var i = 0; i < _updatePipelineBehaviors.Count; i++)
            {
                _updatePipelineBehaviors[i]?.Update();
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
            if (isReal) return GetPipeline(type)?.realVolume ?? 0;
            return GetPipeline(type)?.volume ?? 0;
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
            var pipeline = GetPipeline(type);
            if (pipeline == null) return;
            pipeline.volume = newValue;
        }

        /// <summary>
        /// 检查指定类型音频是否静音。
        /// Check whether the specified audio source type is muted.
        /// </summary>
        /// <param name="type">音频类型 / Type of audio source</param>
        /// <returns>是否静音 / Whether it is muted</returns>
        public bool IsMute(AudioSourceType type)
        {
            return GetPipeline(type)?.realMute ?? true;
        }

        /// <summary>
        /// 静音指定类型音频。
        /// Mute the specified audio source type.
        /// </summary>
        /// <param name="type">音频类型 / Type of audio source</param>
        public void Mute(AudioSourceType type)
        {
            var pipeline = GetPipeline(type);
            if (pipeline == null) return;
            pipeline.mute = true;
        }

        /// <summary>
        /// 取消静音指定类型音频。
        /// Unmute the specified audio source type.
        /// </summary>
        /// <param name="type">音频类型 / Type of audio source</param>
        public void Unmute(AudioSourceType type)
        {
            var pipeline = GetPipeline(type);
            if (pipeline == null) return;
            pipeline.mute = false;
        }

        public bool IsPlaying(AudioSourceType type)
        {
            var pipeline = GetPipeline(type);
            return pipeline?.isPlaying ?? false;
        }

        public void Pause(AudioSourceType type)
        {
            GetPipeline(type)?.Pause();
        }

        public void Resume(AudioSourceType type, bool force = false)
        {
            GetPipeline(type)?.Resume(force);
        }

        public void ClearAudio(AudioSourceType type)
        {
            GetPipeline(type)?.Clear();
        }
        
        public bool RemoveClip(AudioSourceType type, string clipRef)
        {
            return _masterPipeline?.RemoveRequest((int)type, clipRef) ?? false;
        }
    }
}