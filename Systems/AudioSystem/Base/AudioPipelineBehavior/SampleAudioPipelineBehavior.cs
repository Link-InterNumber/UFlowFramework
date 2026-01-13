using System.Collections.Generic;
using UnityEngine.Audio;

namespace PowerCellStudio
{
    public class SampleAudioPipelineBehavior : IAudioPipelineBehavior
    {
        public SampleAudioPipelineBehavior(AudioPipeline pipeline)
        {
            _pipeline = pipeline;
            _managedAudioSource = new List<LinkAudioSource>();
        }

        public void Dispose()
        {
            ClearRequests();
            _managedAudioSource = null;
        }

        private AudioPipeline _pipeline;
        public AudioPipeline pipeline { get => _pipeline; set => _pipeline = value; }


        private List<LinkAudioSource> _managedAudioSource;

        public void ReceiveRequest(AudioRequest request)
        {
            if (string.IsNullOrEmpty(request.clipPath))
            {
                for (int i = 0; i < _managedAudioSource.Count; i++)
                {
                    var audioSource = _managedAudioSource[i];
                    audioSource.Play(request);
                }
                return;
            }
            var pooledAudioSource = LinkAudioSourceUtils.Get(_pipeline);
            pooledAudioSource.autoDespawn = !request.loop;
            pooledAudioSource.Play(request);
            if (request.loop) _managedAudioSource.Add(pooledAudioSource);
        }

        public void RemoveRequest(string clipPath)
        {
            for (int i = 0; i < _managedAudioSource.Count;)
            {
                var audioSource = _managedAudioSource[i];
                if (audioSource.onGoingRequest.clipPath == clipPath)
                {
                    audioSource.FadeOutAndDespawn();
                    _managedAudioSource.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        public void ClearRequests()
        {
            for (var i = 0; i < _managedAudioSource.Count; i++)
            {
                var audioSource = _managedAudioSource[i];
                audioSource.FadeOutAndDespawn();
            }
            _managedAudioSource.Clear();
        }
        
        public void SetMixGroup(AudioMixerGroup mixGroup)
        {
            for (int i = 0; i < _managedAudioSource.Count;)
            {
                var audioSource = _managedAudioSource[i];
                audioSource.audioSource.outputAudioMixerGroup = mixGroup;
            }
        }

        public bool IsPlaying()
        {
            return _managedAudioSource.Count > 0;
        }

        public void SetVolume(float newValue, float transferTime)
        {
            for (var i = 0; i < _managedAudioSource.Count; i++)
            {
                var audioSource = _managedAudioSource[i];
                audioSource.setVolume = newValue;
                audioSource.Fade(audioSource.currentVolume, transferTime);
            }
        }

        public void SetPitch(float newValue, float transferTime)
        {
            for (var i = 0; i < _managedAudioSource.Count; i++)
            {
                var audioSource = _managedAudioSource[i];
                audioSource.setPitch = newValue;
                audioSource.FadePitch(1f, transferTime);
            }
        }

        public void SetMute(bool mute)
        {
            for (var i = 0; i < _managedAudioSource.Count; i++)
            {
                var audioSource = _managedAudioSource[i];
                audioSource.audioSource.mute = mute;
            }
        }

        public void Pause()
        {
            for (var i = 0; i < _managedAudioSource.Count; i++)
            {
                var audioSource = _managedAudioSource[i];
                audioSource.Pause();
            }
        }

        public void Resume()
        {
            for (var i = 0; i < _managedAudioSource.Count; i++)
            {
                var audioSource = _managedAudioSource[i];
                audioSource.Resume();
            }
        }
    }
}