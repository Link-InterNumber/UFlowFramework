using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class CutoffAudioPipelineBehavior : IAudioPipelineBehavior
    {
        public CutoffAudioPipelineBehavior() //(AudioPipeline pipeline)
        {
            // _pipeline = pipeline;
            _assetLoader = AssetUtils.SpawnLoader("CutoffAudioPipelineBehavior");
            _audioSourceCtrl = LinkAudioSourceUtils.Get();
            _audioSourceCtrl.autoDespawn = false;
            _audioSourceCtrl.onReachEnd += OnReachEnd;
        }
        
        public void Dispose()
        {
            AssetUtils.DeSpawnLoader(_assetLoader);
            _audioSourceCtrl.DeSpawn();
        }

        private LinkAudioSource _audioSourceCtrl;
        private IAssetLoader _assetLoader;
        
        public void ReceiveRequest(AudioRequest request)
        {
            if (!string.IsNullOrEmpty(_audioSourceCtrl.onGoingRequest.clipPath))
            {
                _assetLoader.Release(_audioSourceCtrl.onGoingRequest.clipPath);
            }
            _audioSourceCtrl.Clear();
            _assetLoader.LoadAsync<AudioClip>(request.clipPath, (clip) =>
            {
                _audioSourceCtrl.Play(request, clip);
            });
        }

        private void OnReachEnd(string clipPath)
        {
            _assetLoader.Release(clipPath);
            _audioSourceCtrl.Clear();
        }

        public void ClearRequests()
        {
            // do nothing...
        }

        public void Pause()
        {
            _audioSourceCtrl.Pause();
        }
        public void Resume()
        {
            _audioSourceCtrl.Resume();
        }

        public void SetMute(bool mute)
        {
            _audioSourceCtrl.audioSource.mute = mute;
        }

        public void SetPitch(float newValue, float transferTime)
        {
            _audioSourceCtrl.setPitch = newValue;
            _audioSourceCtrl.FadePitch(1f, transferTime);
        }

        public void SetVolume(float newValue, float transferTime)
        {
            _audioSourceCtrl.setVolume = newValue;
            _audioSourceCtrl.Fade(_audioSourceCtrl.currentVolume, transferTime);
        }
    }
}