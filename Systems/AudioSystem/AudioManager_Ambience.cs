namespace PowerCellStudio
{
    public partial class AudioManager
    {
        private IBgmPlayer _ambiencePlayer;

        /// <summary>
        /// 播放环境音频，可选择是否循环并设置淡入/淡出/间隔时间。
        /// Play an ambience audio clip, optionally looped, with configurable fade and interval times.
        /// </summary>
        /// <param name="clipRef">音频引用 / Clip reference</param>
        /// <param name="isLoop">是否循环播放，默认为 true / Whether to loop playback; default true</param>
        /// <param name="fadeoutTime">淡出时间（秒）/ Fade-out time in seconds</param>
        /// <param name="intervalTime">片段间隔时间（秒）/ Interval time between clips in seconds</param>
        /// <param name="fadeinTime">淡入时间（秒）/ Fade-in time in seconds</param>
        public void PlayAmbience(string clipRef, bool isLoop = true,
            float fadeoutTime = 1f, float intervalTime = 1f, float fadeinTime = 1f)
        {
            if(string.IsNullOrEmpty(clipRef)) return;
            var request = new AudioRequest(clipRef, (int)AudioSourceType.Ambience, isLoop)
            {
                fadeIn = fadeinTime,
                fadeOut = fadeoutTime,
                delay = intervalTime
            };
            PushRequest(request);
        }
    }
}