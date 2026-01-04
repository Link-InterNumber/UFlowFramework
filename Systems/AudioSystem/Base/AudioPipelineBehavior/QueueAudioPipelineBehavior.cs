using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class QueueAudioPipelineBehavior : IAudioPipelineBehavior
    {
        public QueueAudioPipelineBehavior(AudioPipeline pipeline)
        {
            _pipeline = pipeline;
            _requestQueue = new Queue<AudioRequest>();
            _assetLoader = AssetUtils.SpawnLoader("QueueAudioPipelineBehavior");
            _audioSorceCtrl.onReachEnd += OnReachEnd;
        }

        private LinkAudioSource _audioSorceCtrl;
        private IAssetLoader _assetLoader;

        private AudioPipeline _pipeline;
        public AudioPipeline pipeline { get => _pipeline; set => _pipeline = value; }

        private Queue<AudioRequest> _requestQueue;
        public void ReceiveRequest(AudioRequest request)
        {
            _requestQueue.Enqueue(request);
            if (string.IsNullOrEmpty(_audioSorceCtrl.onGoingRequest.clipPath))
            {
                _assetLoader.LoadAsync<AudioClip>(request.clipPath, (clip) =>
                {
                    if (clip != null)
                    {
                        _audioSorceCtrl.Play(request, clip);
                    }
                });
            }
        }

        private void OnReachEnd(string clipPath)
        {
            
            if (_requestQueue.Count > 0)
            {
                _assetLoader.Release(_audioSorceCtrl.onGoingRequest.clipPath);
                var nextRequest = _requestQueue.Dequeue();
                _assetLoader.LoadAsync<AudioClip>(nextRequest.clipPath, (clip) =>
                {
                    if (clip != null)
                    {
                        _audioSorceCtrl.Play(nextRequest, clip);
                    }
                });
                if (nextRequest.loop)
                {
                    _requestQueue.Enqueue(nextRequest);
                }
            }
        }

        public void ClearRequests()
        {
            _audioSorceCtrl.Fade(0f, 0.5f, () =>
            {
                _audioSorceCtrl.audioSource.clip = null;
                while (_requestQueue.Count > 0)
                {
                    var request = _requestQueue.Dequeue();
                    _assetLoader.Release(request.clipPath);
                }
            });
        }

        public void Pause()
        {
            _audioSorceCtrl.Pause();
        }
        public void Resume()
        {
            _audioSorceCtrl.Resume();
        }

        public void SetMute(bool mute)
        {
            _audioSorceCtrl.audioSource.mute = mute;
        }

        public void SetPitch(float newValue, float transferTime)
        {
            _audioSorceCtrl.setPitch = newValue;
            _audioSorceCtrl.FadePitch(1f, transferTime);
        }

        public void SetVolume(float newValue, float transferTime)
        {
            _audioSorceCtrl.setVolume = newValue;
            _audioSorceCtrl.Fade(_audioSorceCtrl.currentVolume, transferTime);
        }
    }
}