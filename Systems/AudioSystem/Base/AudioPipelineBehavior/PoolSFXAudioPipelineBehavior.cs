using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace PowerCellStudio
{
    public class PoolSFXAudioPipelineBehavior : IAudioPipelineBehavior, IUpdatePipelineBehavior
    {
        public PoolSFXAudioPipelineBehavior(AudioPipeline pipeline)
        {
            _pipeline = pipeline;
            _audioRequests = new LinkedList<AudioRequest>();
            _SFXCaches = new LinkedList<SFXCache>();
            _onGoingRequestSet = new HashSet<string>();

        }
        
        public void Dispose()
        {
            ClearRequests();
            _audioRequests = null;
            _SFXCaches.Clear();
            _SFXCaches = null;
            _onGoingRequestSet.Clear();
            _onGoingRequestSet = null;
        }

        private AudioPipeline _pipeline;
        public AudioPipeline pipeline { get => _pipeline; set => _pipeline = value; }
        
        private struct SFXCache
        {
            public string clipPath;
            public float removeTime;
        }
        
        public float effectIntervalTime = 0.1f;
        
        // 队列和集合用于管理音频请求
        private LinkedList<AudioRequest> _audioRequests;
        private LinkedList<SFXCache> _SFXCaches;
        private HashSet<string> _onGoingRequestSet;

        public void ReceiveRequest(AudioRequest request)
        {
            if (string.IsNullOrEmpty(request.clipPath))
            {
                return;
            }
            _audioRequests.AddLast(request);
        }
        
        public void RemoveRequest(string clipPath)
        {
            var node = _audioRequests.First;
            while (node != null)
            {
                var nextNode = node.Next;
                if (node.Value.clipPath.Equals(clipPath))
                {
                    _audioRequests.Remove(node);
                }
                node = nextNode;
            }
        }

        public void ClearRequests()
        {
            _audioRequests.Clear();
        }
        
        public void SetMixGroup(AudioMixerGroup mixGroup)
        {
            // do nothing.
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

        public bool IsPlaying()
        {
            return _audioRequests.Count > 0;
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
            var audioSourceCtrl = LinkAudioSourceUtils.Get(_pipeline);
            audioSourceCtrl.autoDespawn = true;
            audioRequest.loop = false;
            audioSourceCtrl.Play(audioRequest);
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
                var next = _audioRequests.First.Value;
                _audioRequests.RemoveFirst();
                if (!_onGoingRequestSet.Add(next.clipPath)) continue;
                var newQuest = new SFXCache()
                {
                    clipPath = next.clipPath,
                    removeTime = currentTime + effectIntervalTime,
                };
                _SFXCaches.AddLast(newQuest);
                PostRequest(next);
            }
        }
    }
}