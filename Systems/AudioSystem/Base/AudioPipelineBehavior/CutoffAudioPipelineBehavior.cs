using UnityEngine;

namespace PowerCellStudio
{
    public class CutoffAudioPipelineBehavior : IAudioPipelineBehavior
    {
        public CutoffAudioPipelineBehavior(AudioPipeline pipeline)
        {
            _pipeline = pipeline;
            _assetLoader = AssetUtils.SpawnLoader("CutoffAudioPipelineBehavior");
            _audioSourceCtrl = LinkAudioSourceUtils.Get(_pipeline);
            _audioSourceCtrl.autoDespawn = false;
            _audioSourceCtrl.onReachEnd += OnReachEnd;
        }
        
        public void Dispose()
        {
            AssetUtils.DeSpawnLoader(_assetLoader);
            _assetLoader = null;
            _audioSourceCtrl.DeSpawn();
            _audioSourceCtrl = null;
        }

        private AudioPipeline _pipeline;
        public AudioPipeline pipeline { get => _pipeline; set => _pipeline = value; }

        private LinkAudioSource _audioSourceCtrl;
        private IAssetLoader _assetLoader;

        public void ReceiveRequest(AudioRequest request)
        {
            if (string.IsNullOrEmpty(request.clipPath))
            {
                if (request.fadeOut > 0)
                {
                    _audioSourceCtrl.Fade(0, request.fadeOut);
                }
                else if (request.fadeIn > 0)
                {
                    _audioSourceCtrl.Fade(_audioSourceCtrl.onGoingRequest.volume, request.fadeIn);
                }
                return;
            }
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
        
        public void RemoveRequest(string clipPath)
        {
            if (_audioSourceCtrl.onGoingRequest.clipPath != clipPath)
            {
                return;
            }
            var currentClipPath = _audioSourceCtrl.onGoingRequest.clipPath;
            _audioSourceCtrl.Clear();
            _assetLoader.Release(currentClipPath);
        }

        private void OnReachEnd(string clipPath, bool isLoop)
        {
            if (isLoop) return;
            _assetLoader.Release(clipPath);
            _audioSourceCtrl.Clear();
        }

        public void ClearRequests()
        {
            var currentClipPath = _audioSourceCtrl.onGoingRequest.clipPath;
            if (string.IsNullOrEmpty(currentClipPath)) return;
            _audioSourceCtrl.Clear();
            _assetLoader.Release(currentClipPath);
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