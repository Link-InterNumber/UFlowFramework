using System;
using UnityEngine;
using UnityEngine.Audio;

namespace PowerCellStudio
{
    public interface IAudioMixerGroupCtrl
    {
        public AudioMixerGroup audioMixerGroup { get; }
        public void TransitionToSnapshot(string snapshot, float timeToReach);
        public void TransitionToSnapshot(AudioMixerSnapshot snapshot, float timeToReach);
        public void SetFloat(string name, float value);
        public float GetFloat(string name);
    }

    #if UNITY_EDITOR
    [Serializable]
    #endif
    public struct AudioRequest
    {
        public AudioRequest(int pipelineId, float fadeOut)
        {
            clipPath = null;
            this.pipelineId = pipelineId;
            loop = false;
            loopTimes = 0;
            position = Vector3.zero;
            volume = 1f;
            fadeIn = 0;
            delay = 0f;
            this.fadeOut = fadeOut;
            full3D = false;
            attachGameObject = null;
        }

        public AudioRequest(string clipPath, int pipelineId, bool isLoop)
        {
            this.clipPath = clipPath;
            this.pipelineId = pipelineId;
            loop = isLoop;
            loopTimes = isLoop ? -1 : 0;
            position = Vector3.zero;
            volume = 1f;
            fadeIn = 0f;
            fadeOut = 0f;
            delay = 0f;
            full3D = false;
            attachGameObject = null;
        }
        public string clipPath;
        public int pipelineId;
        [Range(0, 1)]
        public float volume;
        [Min(0)]
        public float fadeIn;
        [Min(0)]
        public float fadeOut;
        public float delay;
        public bool loop;
        public int loopTimes;
        public bool full3D;
        public Vector3 position;
        public GameObject attachGameObject;
    }

    public interface IUpdatePipelineBehavior
    {
        void Update();
    }

    public interface IAudioPipelineBehavior: IDisposable
    {
        public AudioPipeline pipeline { get; set; }
        public void ReceiveRequest(AudioRequest request);

        public void RemoveRequest(string clipPath);

        public void ClearRequests();
        
        public void SetMixGroup(AudioMixerGroup mixGroup);
        
        public bool IsPlaying();

        public void SetVolume(float newValue, float transferTime);

        public void SetPitch(float newValue, float transferTime);

        public void SetMute(bool mute);

        public void Pause();

        public void Resume();
    }
}