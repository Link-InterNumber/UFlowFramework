using System.Linq;

namespace PowerCellStudio
{
    public partial class AudioManager
    {
        /// <summary>
        /// 检查指定音乐分组是否已注册。
        /// Check whether the specified music group is registered.
        /// </summary>
        /// <param name="group">音乐分组 / Music group</param>
        /// <returns>如果已注册则返回 true，否则返回 false / True if registered; otherwise false</returns>
        public bool HasMusicGroupRegister(MusicGroup group)
        {
            var pipelineId = (int)AudioSourceType.Music * 1000 + (int)group;
            var pipeline = FindPipeline(pipelineId, _masterPipeline);
            return pipeline != null;
        }

        /// <summary>
        /// 播放单个背景音乐片段。
        /// Play a single background music clip (convenience overload).
        /// </summary>
        /// <param name="clipRef">音频引用 / Clip reference</param>
        /// <param name="group">音乐分组 / Music group</param>
        /// <param name="fadeoutTime">淡出时间（秒）/ Fade-out time in seconds</param>
        /// <param name="intervalTime">片段间隔时间（秒）/ Interval time between clips in seconds</param>
        /// <param name="fadeinTime">淡入时间（秒）/ Fade-in time in seconds</param>
        public void PlayMusic(string clipRef, MusicGroup group,
            float fadeoutTime = 0.5f, float intervalTime = 0.3f, float fadeinTime = 0.5f)
        {
            if(string.IsNullOrEmpty(clipRef)) return;
            PlayMusic(new[] {clipRef}, group, false, false, fadeoutTime, intervalTime, fadeinTime);
        }

        private void PlayMusic(string[] clipsRefs, MusicGroup group, bool randPlay, bool restart,
            float fadeoutTime = 0.5f, float intervalTime = 0.3f, float fadeinTime = 0.5f)
        {
            if (clipsRefs == null || clipsRefs.Length == 0) return;
            var pipelineId = (int)AudioSourceType.Music * 1000 + (int)group;
            var pipeline = FindPipeline(pipelineId, _masterPipeline);
            if (pipeline == null)
            {
                pipeline = new AudioPipeline();
                pipeline.Init(pipelineId, new CycleBGMAudioPipelineBehavior(pipeline));
                pipeline.parent = GetPipeline(AudioSourceType.Music);
                _updatePipelineBehaviors.Add(pipeline.updateBehavior);
            }

            if (pipeline.behavior is CycleBGMAudioPipelineBehavior behavior)
            {
                if (behavior.musicRecord.IsSame(clipsRefs) && !restart)
                    return;
                pipeline.Clear();
                var requests = clipsRefs.Select(o => new AudioRequest(o, pipelineId, true)
                {
                    fadeIn = fadeinTime,
                    fadeOut = fadeoutTime,
                    delay = intervalTime
                }).ToArray();
                behavior.musicRecord.SetClips(requests, randPlay);
                PushRequest(requests[0]);
            }
            else
            {
                pipeline.Clear();
                for (var i = 0; i < clipsRefs.Length; i++)
                {
                    var clipRef = clipsRefs[i];
                    var request = new AudioRequest(clipRef, pipelineId, true)
                    {
                        fadeIn = fadeinTime,
                        fadeOut = fadeoutTime,
                        delay = intervalTime
                    };
                    PushRequest(request);
                }
            }
        }

        /// <summary>
        /// 切换当前活跃的音乐分组：恢复匹配分组并暂停其他分组。
        /// Switch the active music group: resume the matching group and pause others.
        /// </summary>
        /// <param name="group">目标音乐分组 / Target music group</param>
        /// <returns>如果找到了匹配分组并恢复则返回 true，否则返回 false / True if a matching group was found and resumed; otherwise false</returns>
        public bool SwitchMusicGroup(MusicGroup group)
        {
            var musicRoot = GetPipeline(AudioSourceType.Music);
            var result = false;
            foreach (var childrenValue in musicRoot.children.Values)
            {
                var pipelineId = (int)AudioSourceType.Music * 1000 + (int)group;
                if (childrenValue.pipelineId == pipelineId)
                {
                    childrenValue.Resume(true);
                    result = true;
                }
                else
                {
                    childrenValue.Pause();
                }
            }
            return result;
        }

        /// <summary>
        /// 释放指定音乐分组的播放管线及其资源。
        /// Dispose the playback pipeline and resources for the specified music group.
        /// </summary>
        /// <param name="group">目标音乐分组 / Target music group</param>
        public void DisposeMusicGroup(MusicGroup group)
        {
            var musicRoot = GetPipeline(AudioSourceType.Music);
            foreach (var childrenValue in musicRoot.children.Values)
            {
                var pipelineId = (int)AudioSourceType.Music * 1000 + (int)group;
                if (childrenValue.pipelineId != pipelineId) continue;
                childrenValue.parent = null;
                _updatePipelineBehaviors.Remove(childrenValue.updateBehavior);
                childrenValue.Dispose();
            }
        }
    }
}
