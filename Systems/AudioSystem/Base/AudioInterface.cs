using System;
using System.Collections.Generic;
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

    public struct AudioRequest
    {
        public AudioRequest(AudioSourceType audioType, float fadeOut)
        {
            clipPath = null;
            this.audioType = audioType;
            loop = false;
            position = Vector3.zero;
            volume = 1f;
            fadeIn = 0;
            this.fadeOut = fadeOut;
            full3D = false;
            attachGameObject = null;
        }
        
        public AudioRequest(string clipPath, AudioSourceType audioType, bool isLoop)
        {
            this.clipPath = clipPath;
            this.audioType = audioType;
            loop = isLoop;
            position = Vector3.zero;
            volume = 1f;
            fadeIn = 0f;
            fadeOut = 0f;
            full3D = false;
            attachGameObject = null;
        }
        public string clipPath;
        public AudioSourceType audioType;
        [Range(0, 1)]
        public float volume;
        [Min(0)]
        public float fadeIn;
        [Min(0)]
        public float fadeOut;
        public bool loop;
        public bool full3D;
        public Vector3 position;
        public GameObject attachGameObject;
    }

    // public interface IAudioPipeline
    // {
    //     public void Init(AudioSourceType type, IAudioPipelineBehavior behavior);
    //     public IAudioMixerGroupCtrl mixCtrl { get; set; }
    //     public AudioSourceType audioType { get; }
    //     public float volume { get; set; }
    //     public float pitch { get; set; }
    //     public bool mute { get; set; }
    //     public float realVolume { get; }
    //     public float realPitch { get; }
    //     public bool realMute { get; }
    //     public IAudioPipeline parent { get; set; }
    //     public Dictionary<AudioSourceType, IAudioPipeline> children { get; }
    //     public void UpdateRealProperties();
    //     public void UpdateChildrenProperties();
    //     public bool PushRequest(AudioRequest request);
    //     public void Clear();
    // }

    public interface IUpdatePipelineBehavior
    {
        void Update();
    }

    public interface IAudioPipelineBehavior: IDisposable
    {
        public AudioPipeline pipeline { get; set; }
        public void ReceiveRequest(AudioRequest request);

        public void ClearRequests();

        public void SetVolume(float newValue, float transferTime);

        public void SetPitch(float newValue, float transferTime);

        public void SetMute(bool mute);

        public void Pause();

        public void Resume();
    }
}