using System.Linq;

namespace PowerCellStudio
{
    public partial class AudioManager
    {
        public bool HasMusicGroupRegister(MusicGroup group)
        {
            var pipelineId = (int)AudioSourceType.Music * 1000 + (int)group;
            var pipeline = FindPipeline(pipelineId, _masterPipeline);
            return pipeline != null;
        }

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