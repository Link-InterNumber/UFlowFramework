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
            if (transferTime <= 0)
                pipeline.volume = newValue;
            else
                pipeline.SetVolume(newValue, transferTime);

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

        /// <summary>
        /// 检查指定类型音频是否正在播放。
        /// Check whether the specified audio source type is currently playing.
        /// </summary>
        /// <param name="type">音频类型 / Type of audio source</param>
        /// <returns>如果正在播放则返回 true，否则返回 false / True if playing; otherwise false</returns>
        public bool IsPlaying(AudioSourceType type)
        {
            var pipeline = GetPipeline(type);
            return pipeline?.isPlaying ?? false;
        }

        /// <summary>
        /// 暂停指定类型的音频播放。
        /// Pause playback for the specified audio source type.
        /// </summary>
        /// <param name="type">音频类型 / Type of audio source</param>
        public void Pause(AudioSourceType type)
        {
            GetPipeline(type)?.Pause();
        }

        /// <summary>
        /// 恢复指定类型的音频播放。
        /// Resume playback for the specified audio source type.
        /// </summary>
        /// <param name="type">音频类型 / Type of audio source</param>
        /// <param name="force">是否强制恢复（忽略内部条件）/ Whether to force resume (ignore internal conditions)</param>
        public void Resume(AudioSourceType type, bool force = false)
        {
            GetPipeline(type)?.Resume(force);
        }

        /// <summary>
        /// 清空指定类型音频的播放队列和资源。
        /// Clear the playback queue and resources for the specified audio source type.
        /// </summary>
        /// <param name="type">音频类型 / Type of audio source</param>
        public void ClearAudio(AudioSourceType type)
        {
            GetPipeline(type)?.Clear();
        }

        /// <summary>
        /// 从指定类型的音频管线中移除指定引用的音频片段。
        /// Remove a clip by reference from the specified audio source type's pipeline.
        /// </summary>
        /// <param name="type">音频类型 / Type of audio source</param>
        /// <param name="clipRef">音频片段引用 / Clip reference</param>
        /// <returns>移除成功返回 true，否则返回 false / True if removal succeeded; otherwise false</returns>
        public bool RemoveClip(AudioSourceType type, string clipRef)
        {
            return _masterPipeline?.RemoveRequest((int)type, clipRef) ?? false;
        }
    }
}