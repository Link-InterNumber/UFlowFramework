using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class PoolSFXAudioPipelineBehavior : IAudioPipelineBehavior, IUpdatePipelineBehavior
    {
        public PoolSFXAudioPipelineBehavior(AudioPipeline pipeline)
        {
            _pipeline = pipeline;
            _assetLoader = AssetUtils.SpawnLoader("PoolSFXAudioPipelineBehavior");
        }
        
        public void Dispose()
        {
            ClearRequests();
            _SFXCaches.Clear();
            _SFXCaches = null;
            _onGoingRequestSet.Clear();
            _onGoingRequestSet = null;
            AssetUtils.DeSpawnLoader(_assetLoader);
            _assetLoader = null;
        }

        private AudioPipeline _pipeline;
        public AudioPipeline pipeline { get => _pipeline; set => _pipeline = value; }
        
        private struct SFXCache
        {
            public string clipPath;
            public float removeTime;
        }
        
        // public AudioPipeline pipeline { get; set; }
        public float effectIntervalTime = 0.1f;
        private IAssetLoader _assetLoader;
        
        // 队列和集合用于管理音频请求
        private Queue<AudioRequest> _audioRequests;
        private LinkedList<SFXCache> _SFXCaches;
        private HashSet<string> _onGoingRequestSet;

        public void ReceiveRequest(AudioRequest request)
        {
            if (string.IsNullOrEmpty(request.clipPath))
            {
                return;
            }
            _audioRequests.Enqueue(request);
        }
        
        public void RemoveRequest(string clipPath)
        {
            
        }

        public void ClearRequests()
        {
            _audioRequests.Clear();
        }

        // private float _volume;
        public void SetVolume(float newValue, float transferTime)
        {
            // _volume = newValue;
        }

        // private float _pitch = 1;
        public void SetPitch(float newValue, float transferTime)
        {
            // _pitch = newValue;
        }

        private bool _isMute;
        public void SetMute(bool mute)
        {
            _isMute = mute;
        }
        
        private bool _isPaused;
        public void Pause()
        {
            _isPaused = true;
        }

        public void Resume()
        {
            _isPaused = false;
        }

        private void PostRequest(AudioRequest audioRequest)
        {
            if (_isMute) return;
            _assetLoader.LoadAsync<AudioClip>(audioRequest.clipPath, (clip) =>
            {
                var audioSourceCtrl = LinkAudioSourceUtils.Get(_pipeline);
                audioSourceCtrl.autoDespawn = true;
                audioRequest.loop = false;
                audioSourceCtrl.Play(audioRequest, clip);
                audioSourceCtrl.onReachEnd += OnReachEnd;
            });
        }
        
        private void OnReachEnd(string clipPath, bool isLoop)
        {
            _assetLoader.Release(clipPath);
        }

        public void Update()
        {
            if(_isPaused || _SFXCaches == null || _audioRequests == null) return;
            var currentTime = Time.unscaledTime;
            
            var cache = _SFXCaches.First;
            while (cache != null)
            {
                var nextNode = cache.Next;
                if (cache.Value.removeTime < currentTime)
                {
                    _onGoingRequestSet.Remove(cache.Value.clipPath);
                    _SFXCaches.Remove(cache);
                }
                cache = nextNode;
            }

            while (_audioRequests.Count > 0)
            {
                var poped = _audioRequests.Dequeue();
                if (!_onGoingRequestSet.Add(poped.clipPath)) continue;
                var newQuest = new SFXCache()
                {
                    clipPath = poped.clipPath,
                    removeTime = currentTime + effectIntervalTime,
                };
                _SFXCaches.AddLast(newQuest);
                PostRequest(poped);
            }
        }
    }
}