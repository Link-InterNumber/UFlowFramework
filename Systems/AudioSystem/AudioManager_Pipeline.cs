using System;
using UnityEngine;

namespace PowerCellStudio
{
    public partial class AudioManager
    {
        private AudioPipeline _masterPipeline;

        private void BuildPipeLine()
        {
            _masterPipeline = new AudioPipeline();
            _masterPipeline.Init((int)AudioSourceType.Master, null);

            var bgmPipeline = new AudioPipeline();
            bgmPipeline.Init((int)AudioSourceType.Music, new CycleBGMAudioPipelineBehavior(bgmPipeline));
            bgmPipeline.parent = _masterPipeline;

            var dialogPipeline = new AudioPipeline();
            dialogPipeline.Init((int)AudioSourceType.Dialog, new CutoffAudioPipelineBehavior(dialogPipeline));
            dialogPipeline.parent = _masterPipeline;
            
            var SFXPipeline = new AudioPipeline();
            SFXPipeline.Init((int)AudioSourceType.SFX, null);
            SFXPipeline.parent = _masterPipeline;

            var SFXUiPipeline = new AudioPipeline();
            SFXUiPipeline.Init((int)AudioSourceType.SFXUI, new PoolSFXAudioPipelineBehavior(SFXUiPipeline));
            SFXUiPipeline.parent = SFXPipeline;

            var SFX3DPipeline = new AudioPipeline();
            SFX3DPipeline.Init((int)AudioSourceType.SFX3D, new PoolSFXAudioPipelineBehavior(SFX3DPipeline));
            SFX3DPipeline.parent = SFXPipeline;

            var ambiencePipeline = new AudioPipeline();
            ambiencePipeline.Init((int)AudioSourceType.Ambience, new SampleAudioPipelineBehavior(ambiencePipeline));
            ambiencePipeline.parent = SFXPipeline;

            var systemPipeline = new AudioPipeline();
            systemPipeline.Init((int)AudioSourceType.System, new SampleAudioPipelineBehavior(systemPipeline));
            systemPipeline.parent = _masterPipeline;

            // TODO CN:加载mixer并用AudioMixerGroupCtrl封装，赋值给AudioPipeline.mixCtrl
            // TODO EN:Load mixer and wrap it with AudioMixerGroupCtrl, assign it to AudioPipeline.mixCtrl
        }

        private AudioPipeline GetPipeline(AudioSourceType type)
        {
            return FindPipeline((int)type, _masterPipeline);
        }
        
        private AudioPipeline FindPipeline(int type, AudioPipeline currentPipeline)
        {
            if (currentPipeline.pipelineId == type)
                return currentPipeline;
            if (currentPipeline.children.TryGetValue(type, out var pipeline))
                return pipeline;
            foreach (var child in currentPipeline.children.Values)
            {
                var result = FindPipeline(type, child);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}