using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class SampleAudioPipelineBehavior : IAudioPipelineBehavior
    {
        public SampleAudioPipelineBehavior(AudioPipeline pipeline)
        {
            _pipeline = pipeline;
            _assetLoader = AssetUtils.SpawnLoader("SampleAudioPipelineBehavior");
            _manageredAudioSource = new List<LinkAudioSource>();
        }

        public void Dispose()
        {
            ClearRequests();
            _manageredAudioSource = null;
            AssetUtils.DeSpawnLoader(_assetLoader);
            _assetLoader = null;
        }

        private AudioPipeline _pipeline;
        public AudioPipeline pipeline { get => _pipeline; set => _pipeline = value; }

        private IAssetLoader _assetLoader;

        private List<LinkAudioSource> _manageredAudioSource;

        public void ReceiveRequest(AudioRequest request)
        {
            if (string.IsNullOrEmpty(request.clipPath))
            {
                if (request.fadeOut > 0)
                {
                    for (int i = 0; i < _manageredAudioSource.Count; i++)
                    {
                        var audioSource = _manageredAudioSource[i];
                        audioSource.Fade(0, request.fadeOut);
                    }
                }
                else if (request.fadeIn > 0)
                {
                    for (int i = 0; i < _manageredAudioSource.Count; i++)
                    {
                        var audioSource = _manageredAudioSource[i];
                        audioSource.Fade(audioSource.onGoingRequest.volume, request.fadeIn);
                    }
                }
                return;
            }
            _assetLoader.LoadAsync<AudioClip>(request.clipPath, (clip) =>
            {
                var audioSource = pipeline.GetAudioSource();
                audioSource.autoDespawn = true;
                audioSource.Play(request, clip);
                audioSource.onReachEnd += OnReachEnd;
                if (request.loop) _manageredAudioSource.Add(audioSource);
            });
        }

        private void OnReachEnd(string clipPath, bool isLoop)
        {
            if (isLoop) return;
            _assetLoader.Release(clipPath);
        }

        public void ClearRequests()
        {
            for (int i = 0; i < _manageredAudioSource.Count; i++)
            {
                var audioSource = _manageredAudioSource[i];
                _assetLoader.Release(audioSource.onGoingRequest.clipPath);
                audioSource.Clear();
            }
            _manageredAudioSource.Clear();
        }

        public void SetVolume(float newValue, float transferTime)
        {
            for (int i = 0; i < _manageredAudioSource.Count; i++)
            {
                var audioSource = _manageredAudioSource[i];
                audioSource.setVolume = newValue;
                audioSource.Fade(audioSource.onGoingRequest.volume, transferTime);
            }
        }

        public void SetPitch(float newValue, float transferTime)
        {
            for (int i = 0; i < _manageredAudioSource.Count; i++)
            {
                var audioSource = _manageredAudioSource[i];
                audioSource.setPitch = newValue;
                audioSource.Fade(1f, transferTime);
            }
        }

        public void SetMute(bool mute)
        {
            for (int i = 0; i < _manageredAudioSource.Count; i++)
            {
                var audioSource = _manageredAudioSource[i];
                audioSource.audioSource.mute = mute;
            }
        }

        public void Pause()
        {
            for (int i = 0; i < _manageredAudioSource.Count; i++)
            {
                var audioSource = _manageredAudioSource[i];
                audioSource.Pause();
            }
        }

        public void Resume()
        {
            for (int i = 0; i < _manageredAudioSource.Count; i++)
            {
                var audioSource = _manageredAudioSource[i];
                audioSource.Resume();
            }
        }
    }
}