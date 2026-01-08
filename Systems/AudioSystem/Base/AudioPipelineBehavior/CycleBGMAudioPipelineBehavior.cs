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
                var clonedRequest = nextRequest;
                clonedRequest.loop = false;
                if (_audioSourceCtrl == null || !_audioSourceCtrl.gameObject.activeInHierarchy)
                {
                    _audioSourceCtrl = LinkAudioSourceUtils.Get(_pipeline);
                    _audioSourceCtrl.autoDespawn = true;
                    _audioSourceCtrl.onRemoveClip += OnRemoveClip;
                }
                _audioSourceCtrl.Play(clonedRequest, clip);
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
            if (_requestQueue.Count <= 0)
            {
                _audioSourceCtrl.FadeOutAndDespawn();
                _audioSourceCtrl = null;
            }
            else 
                TryPostRequest();
        }

        private void OnRemoveClip(string clipPath, bool onDespawn)
        {
            if (onDespawn)
            {
                _audioSourceCtrl = null;
                TryPostRequest();
            }
            _assetLoader.Release(clipPath);
        }

        public void ClearRequests()
        {
            _audioSourceCtrl.FadeOutAndDespawn();
            _audioSourceCtrl = null;
            _requestQueue.Clear();
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