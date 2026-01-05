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
            _masterPipeline.Init(AudioSourceType.Master, null);

            var bgmPipeline = new AudioPipeline();
            bgmPipeline.Init(AudioSourceType.Music, new CycleBGMAudioPipelineBehavior(bgmPipeline));
            bgmPipeline.parent = _masterPipeline;

            var dialogPipeline = new AudioPipeline();
            dialogPipeline.Init(AudioSourceType.Dialog, new CutoffAudioPipelineBehavior(dialogPipeline));
            dialogPipeline.parent = _masterPipeline;

            var UIEffectPipeline = new AudioPipeline();
            UIEffectPipeline.Init(AudioSourceType.UIEffect, new PoolSFXAudioPipelineBehavior(UIEffectPipeline));
            UIEffectPipeline.parent = _masterPipeline;

            var Effect3DPipeline = new AudioPipeline();
            Effect3DPipeline.Init(AudioSourceType.Effect3D, new PoolSFXAudioPipelineBehavior(Effect3DPipeline));
            Effect3DPipeline.parent = UIEffectPipeline;

            var ambiencePipeline = new AudioPipeline();
            ambiencePipeline.Init(AudioSourceType.Ambience, new SampleAudioPipelineBehavior(ambiencePipeline));
            ambiencePipeline.parent = UIEffectPipeline;

            var systemPipeline = new AudioPipeline();
            systemPipeline.Init(AudioSourceType.System, new SampleAudioPipelineBehavior(systemPipeline));
            systemPipeline.parent = _masterPipeline;

            // TODO CN:加载mixer并用AudioMixerGroupCtrl封装，赋值给AudioPipeline.mixCtrl
            // TODO EN:Load mixer and wrap it with AudioMixerGroupCtrl, assign it to AudioPipeline.mixCtrl
        }

        private AudioPipeline GetPipeline(AudioSourceType type)
        {
            return FindPipeline(type, _masterPipeline);
        }
        
        private AudioPipeline FindPipeline(AudioSourceType type, AudioPipeline currentPipeline)
        {
            if (currentPipeline.audioType == type)
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