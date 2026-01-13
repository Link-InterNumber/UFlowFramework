namespace PowerCellStudio
{
    public partial class AudioManager
    {
        /// <summary>
        /// 播放对话音频并在播放完成时可选触发回调。
        /// Play a dialog audio clip and optionally invoke a callback when playback completes.
        /// </summary>
        /// <param name="clipRef">音频片段引用 / Clip reference</param>
        /// <param name="callback">播放完成回调，只有在完整播放后触发 / Playback completion callback, triggered only after full playback</param>
        public void PlayDialog(string clipRef, BaseLinkAction callback = null)
        {
            if (string.IsNullOrEmpty(clipRef)) return;
            var request = new AudioRequest(clipRef, (int)AudioSourceType.Dialog, false);
            var dialogPipeline = GetPipeline(AudioSourceType.Dialog);
            if (dialogPipeline?.behavior is CutoffAudioPipelineBehavior clipBehavior)
            {
                clipBehavior.onCompleted.RemoveAllListeners();
                if (callback != null) clipBehavior.onCompleted.AddListenerOnce(callback);
            }
            dialogPipeline?.PushRequest(request);
        }
    }
}