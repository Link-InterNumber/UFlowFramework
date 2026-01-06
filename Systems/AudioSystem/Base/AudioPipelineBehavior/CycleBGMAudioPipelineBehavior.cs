using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class CycleBGMAudioPipelineBehavior : IAudioPipelineBehavior
    {
        public CycleBGMAudioPipelineBehavior(AudioPipeline pipeline)
        {
            _pipeline = pipeline;
            _requestQueue = new LinkedList<AudioRequest>();
            _assetLoader = AssetUtils.SpawnLoader("QueueAudioPipelineBehavior");
            _audioSourceCtrl = LinkAudioSourceUtils.Get(_pipeline);
            _audioSourceCtrl.autoDespawn = false;
            _audioSourceCtrl.onReachEnd += OnReachEnd;
        }
        
        public void Dispose()
        {
            ClearRequests();
            AssetUtils.DeSpawnLoader(_assetLoader);
            _assetLoader = null;
            _audioSourceCtrl.DeSpawn();
            _audioSourceCtrl = null;
        }

        private LinkAudioSource _audioSourceCtrl;
        private IAssetLoader _assetLoader;

        private AudioPipeline _pipeline;
        public AudioPipeline pipeline { get => _pipeline; set => _pipeline = value; }

        private LinkedList<AudioRequest> _requestQueue;

        private void TryPostRequest()
        {
            if (_requestQueue.Count <= 0) return;
            var nextRequest = _requestQueue.First.Value;
            _requestQueue.RemoveFirst();
            _assetLoader.LoadAsync<AudioClip>(nextRequest.clipPath, (clip) =>
            {
                _audioSourceCtrl.Play(nextRequest, clip);
            });
            if (!nextRequest.loop) return;
            _requestQueue.AddLast(nextRequest);
        }

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
            var node = _requestQueue.First;
            while (node != null)
            {
                var nextNode = node.Next;
                if (node.Value.clipPath == request.clipPath)
                {
                    _requestQueue.Remove(node);
                }
                node = nextNode;
            }
            _requestQueue.AddLast(request);
            if (string.IsNullOrEmpty(_audioSourceCtrl.onGoingRequest.clipPath))
            {
                TryPostRequest();
            }
        }
        
        public void RemoveRequest(string clipPath)
        {
            var node = _requestQueue.First;
            while (node != null)
            {
                var nextNode = node.Next;
                if (node.Value.clipPath == clipPath)
                {
                    _requestQueue.Remove(node);
                }
                node = nextNode;
            }
            if (_audioSourceCtrl.onGoingRequest.clipPath != clipPath) return;
            var currentClipPath = _audioSourceCtrl.onGoingRequest.clipPath;
            _audioSourceCtrl.Clear();
            _assetLoader.Release(currentClipPath);
            TryPostRequest();
        }

        private void OnReachEnd(string clipPath, bool isLoop)
        {
            if (_requestQueue.Count <= 0)
            {
                _assetLoader.Release(clipPath);
                _audioSourceCtrl.Clear();
                return;
            }
            if (_requestQueue.First.Value.clipPath != clipPath)
                _assetLoader.Release(clipPath);
            TryPostRequest();
        }

        public void ClearRequests()
        {
            _audioSourceCtrl.Clear();
            var node = _requestQueue.First;
            while (node != null)
            {
                _assetLoader.Release(node.Value.clipPath);
                node = node.Next;
            }
            _requestQueue.Clear();
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