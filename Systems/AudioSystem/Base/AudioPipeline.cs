using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class AudioPipeline 
    {
        private IAudioPipelineBehavior _behavior;

        public void Init(AudioSourceType type, IAudioPipelineBehavior behavior)
        {
            _audioType = type;
            _behavior = behavior;
            _children = new Dictionary<AudioSourceType, AudioPipeline>();
            _mute = false;
            _realMute = false;
            _volume = 1f;
            _pitch = 1f;
        }
        
        private IAudioMixerGroupCtrl _mixCtrl;
        public IAudioMixerGroupCtrl mixCtrl { get => _mixCtrl; set => _mixCtrl = value; }

        private AudioSourceType _audioType;
        public AudioSourceType audioType => _audioType;

        private float _volume;
        public float volume
        {
            get => _volume;
            set
            {
                _volume = Mathf.Clamp01(value);
                UpdateRealProperties();
                UpdateChildrenProperties();
            } 
        }

        private float _pitch;
        public float pitch
        {
            get => _pitch;
            set
            {
                _pitch = Mathf.Max(value, 0);
                UpdateRealProperties();
                UpdateChildrenProperties();
            }
        }

        private float _realVolume;
        public float realVolume => _realVolume;

        private float _realPitch;
        public float realPitch => _realPitch;

        private bool _mute;
        public bool mute
        {
            get => _mute;
            set
            {
                _mute = value;
                UpdateRealProperties();
                UpdateChildrenProperties();
            }
        }

        private bool _realMute;
        public bool realMute => _realMute;

        private AudioPipeline _parent;
        public AudioPipeline parent
        {
            get => _parent;
            set
            {
                _parent = value;
                if (value != null) 
                    value.children.Add(_audioType, this);
                else 
                    value.children.Remove(_audioType);
                UpdateRealProperties();
                UpdateChildrenProperties();
            }
        }

        private Dictionary<AudioSourceType, AudioPipeline> _children;
        public Dictionary<AudioSourceType, AudioPipeline> children => _children;

        public void UpdateRealProperties()
        {
            _realVolume = _volume * (parent != null ? parent.realVolume : 1f);
            _realPitch = _pitch * (parent != null ? parent.realPitch : 1f);
            _realMute = _mute || (parent != null && parent.realMute);
            _behavior.SetVolume(_realVolume, 0f);
            _behavior.SetPitch(_realPitch, 0f);
            _behavior.SetMute(_realMute);
        }
        
        public void UpdateChildrenProperties()
        {
            foreach (var child in _children.Values)
            {
                child.UpdateRealProperties();
                child.UpdateChildrenProperties();
            }
        }

        public bool PushRequest(AudioRequest request)
        {
            if (request.audioType != _audioType)
            {
                if (_children.TryGetValue(request.audioType, out var childPipeline))
                {
                    childPipeline.PushRequest(request);
                    return true;
                }
                else
                {
                    foreach (var child in _children.Values)
                    {
                        if (child.PushRequest(request)) return true;
                    }
                }
            }
            else
            {
                _behavior.ReceiveRequest(request);
                return true;
            }
            return false;
        }

        private RefCountBool _isPause; 
        public void Pause()
        {
            if (!_isPause)
            {
                _behavior.Pause();
            }
            _isPause++;
        }
        
        public void Resume(bool force)
        {
            if (force)
            {
                _isPause.Clear();
                _behavior.Resume();
                return;
            }
            _isPause--;
            if (!_isPause)
            {
                _behavior.Resume();
            }
        }
        
        public void Clear()
        {
            _behavior.ClearRequests();
            foreach (var child in _children.Values)
            {
                child.Clear();
            }
        }
    }
}