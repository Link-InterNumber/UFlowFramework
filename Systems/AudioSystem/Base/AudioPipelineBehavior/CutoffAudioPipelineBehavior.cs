using UnityEngine;

namespace PowerCellStudio
{
    public class CutoffAudioPipelineBehavior : IAudioPipelineBehavior
    {
        public CutoffAudioPipelineBehavior(AudioPipeline pipeline)
        {
            _pipeline = pipeline;
            _assetLoader = AssetUtils.SpawnLoader("CutoffAudioPipelineBehavior");
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
            _assetLoader.LoadAsync<AudioClip>(request.clipPath, (clip) =>
            {
                if (_audioSourceCtrl == null || !_audioSourceCtrl.gameObject.activeInHierarchy)
                {
                    _audioSourceCtrl = LinkAudioSourceUtils.Get(_pipeline);
                    _audioSourceCtrl.autoDespawn = true;
                    _audioSourceCtrl.onRemoveClip += OnRemoveClip;
                }
                _audioSourceCtrl.Play(request, clip);
            });
        }
        
        public void RemoveRequest(string clipPath)
        {
            if (_audioSourceCtrl == null || _audioSourceCtrl.onGoingRequest.clipPath != clipPath)
            {
                return;
            }
            ClearRequests();
        }

        private void OnRemoveClip(string clipPath, bool onDespawn)
        {
            _assetLoader.Release(clipPath);
            if (onDespawn) _audioSourceCtrl = null;
        }

        public void ClearRequests()
        {
            _audioSourceCtrl?.FadeOutAndDespawn();
            _audioSourceCtrl = null;
        }

        public void Pause()
        {
            _audioSourceCtrl?.Pause();
        }
        public void Resume()
        {
            _audioSourceCtrl?.Resume();
        }

        public void SetMute(bool mute)
        {
            if (_audioSourceCtrl == null) return;
            _audioSourceCtrl.audioSource.mute = mute;
        }

        public void SetPitch(float newValue, float transferTime)
        {
            if (_audioSourceCtrl == null) return;
            _audioSourceCtrl.setPitch = newValue;
            _audioSourceCtrl.FadePitch(1f, transferTime);
        }

        public void SetVolume(float newValue, float transferTime)
        {
            if (_audioSourceCtrl == null) return;
            _audioSourceCtrl.setVolume = newValue;
            _audioSourceCtrl.Fade(_audioSourceCtrl.currentVolume, transferTime);
        }
    }
}