using System.Collections.Generic;
using UnityEngine.Audio;

namespace PowerCellStudio
{
    public partial class AudioManager
    {
        private AudioPipeline _masterPipeline;
        private List<IUpdatePipelineBehavior> _updatePipelineBehaviors = new List<IUpdatePipelineBehavior>();

        private void BuildPipeLine()
        {
            _masterPipeline = new AudioPipeline();
            _masterPipeline.Init((int)AudioSourceType.Master, null);

            var bgmPipeline = new AudioPipeline();
            bgmPipeline.Init((int)AudioSourceType.Music, null); //new CycleBGMAudioPipelineBehavior(bgmPipeline));
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
            _updatePipelineBehaviors.Add(SFXUiPipeline.updateBehavior);

            var SFX3DPipeline = new AudioPipeline();
            SFX3DPipeline.Init((int)AudioSourceType.SFX3D, new PoolSFXAudioPipelineBehavior(SFX3DPipeline));
            SFX3DPipeline.parent = SFXPipeline;
            _updatePipelineBehaviors.Add(SFX3DPipeline.updateBehavior);

            var ambiencePipeline = new AudioPipeline();
            ambiencePipeline.Init((int)AudioSourceType.Ambience, new SampleAudioPipelineBehavior(ambiencePipeline));
            ambiencePipeline.parent = SFXPipeline;

            var systemPipeline = new AudioPipeline();
            systemPipeline.Init((int)AudioSourceType.System, new SampleAudioPipelineBehavior(systemPipeline));
            systemPipeline.parent = _masterPipeline;

            // TODO CN:加载mixer并用AudioMixerGroupCtrl封装，赋值给AudioPipeline.mixCtrl
            // TODO EN:Load mixer and wrap it with AudioMixerGroupCtrl, assign it to AudioPipeline.mixCtrl
        }

        public void AddAudioMixerGroupToPipeline(int pipelineId, string mixerPath, string groupPath)
        {
            _assetLoader.LoadAsync<AudioMixer>(mixerPath, audioMixer =>
            {
                var group = audioMixer.FindMatchingGroups(groupPath);
                if (group.Length == 0) return;
                var pipeline = FindPipeline(pipelineId, _masterPipeline);
                if (pipeline == null) return;
                var mixCtrl = new AudioMixerGroupCtrl(audioMixer, group[0]);
                pipeline.mixCtrl = mixCtrl;
            });
        }

        public bool RemoveAudioMixerGroupFromPipeline(int pipelineId)
        {
            var pipeline = FindPipeline(pipelineId, _masterPipeline);
            if (pipeline == null) return false;
            pipeline.mixCtrl = null;
            return true;
        }

        public IAudioMixerGroupCtrl GetPipelineMixerCtrl(int pipelineId)
        {
            var pipeline = FindPipeline(pipelineId, _masterPipeline);
            return pipeline?.mixCtrl ?? null;
        }

        private AudioPipeline GetPipeline(AudioSourceType type)
        {
            return FindPipeline((int)type, _masterPipeline);
        }
        
        private AudioPipeline FindPipeline(int id, AudioPipeline currentPipeline)
        {
            if (currentPipeline.pipelineId == id)
                return currentPipeline;
            if (currentPipeline.children.TryGetValue(id, out var pipeline))
                return pipeline;
            foreach (var child in currentPipeline.children.Values)
            {
                var result = FindPipeline(id, child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void PushRequest(AudioRequest request)
        {
            if (_masterPipeline == null) return;
            if (_masterPipeline.PushRequest(request)) return;
            ModuleLogger.LogError<AudioManager>($"Play audio request failed, pipeline id: {request.pipelineId}");
        }

        private void DisposePipeline(int pipelineId)
        {
            var pipeline = FindPipeline(pipelineId, _masterPipeline);
            if (pipeline == null) return;
            _updatePipelineBehaviors.Remove(pipeline.updateBehavior);
            pipeline.Dispose();
        }
    }
}