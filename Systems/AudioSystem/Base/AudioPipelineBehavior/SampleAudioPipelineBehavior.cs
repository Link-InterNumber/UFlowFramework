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
            _managedAudioSource = new List<LinkAudioSource>();
        }

        public void Dispose()
        {
            ClearRequests();
            _managedAudioSource = null;
            AssetUtils.DeSpawnLoader(_assetLoader);
            _assetLoader = null;
        }

        private AudioPipeline _pipeline;
        public AudioPipeline pipeline { get => _pipeline; set => _pipeline = value; }

        private IAssetLoader _assetLoader;

        private List<LinkAudioSource> _managedAudioSource;

        public void ReceiveRequest(AudioRequest request)
        {
            if (string.IsNullOrEmpty(request.clipPath))
            {
                if (request.fadeOut > 0)
                {
                    for (int i = 0; i < _managedAudioSource.Count; i++)
                    {
                        var audioSource = _managedAudioSource[i];
                        audioSource.Fade(0, request.fadeOut);
                    }
                }
                else if (request.fadeIn > 0)
                {
                    for (int i = 0; i < _managedAudioSource.Count; i++)
                    {
                        var audioSource = _managedAudioSource[i];
                        audioSource.Fade(audioSource.onGoingRequest.volume, request.fadeIn);
                    }
                }
                return;
            }
            _assetLoader.LoadAsync<AudioClip>(request.clipPath, (clip) =>
            {
                var audioSource = LinkAudioSourceUtils.Get(_pipeline);
                audioSource.autoDespawn = !request.loop;
                audioSource.Play(request, clip);
                audioSource.onRemoveClip += OnRemoveClip;
                if (request.loop) _managedAudioSource.Add(audioSource);
            });
        }

        public void RemoveRequest(string clipPath)
        {
            for (int i = 0; i < _managedAudioSource.Count;)
            {
                var audioSource = _managedAudioSource[i];
                if (audioSource.onGoingRequest.clipPath == clipPath)
                {
                    audioSource.DeSpawn();
                    _assetLoader.Release(clipPath);
                    _managedAudioSource.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        private void OnRemoveClip(string clipPath, bool onDespawn)
        {
            _assetLoader.Release(clipPath);
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

        public void SetVolume(float newValue, float transferTime)
        {
            for (var i = 0; i < _managedAudioSource.Count; i++)
            {
                var audioSource = _managedAudioSource[i];
                audioSource.setVolume = newValue;
                audioSource.Fade(audioSource.onGoingRequest.volume, transferTime);
            }
        }

        public void SetPitch(float newValue, float transferTime)
        {
            for (var i = 0; i < _managedAudioSource.Count; i++)
            {
                var audioSource = _managedAudioSource[i];
                audioSource.setPitch = newValue;
                audioSource.Fade(1f, transferTime);
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