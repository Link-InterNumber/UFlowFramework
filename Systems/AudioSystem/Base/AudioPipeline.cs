using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class AudioPipeline 
    {
        private IAudioPipelineBehavior _behavior;

        public void Init(int id, IAudioPipelineBehavior behavior)
        {
            _pipelineId = id;
            _behavior = behavior;
            _children = new Dictionary<int, AudioPipeline>();
            _mute = false;
            _realMute = false;
            _volume = 1f;
            _pitch = 1f;
            _isPause = new RefCountBool();
        }
        
        private IAudioMixerGroupCtrl _mixCtrl;
        public IAudioMixerGroupCtrl mixCtrl { get => _mixCtrl; set => _mixCtrl = value; }

        private int _pipelineId;
        public int pipelineId => _pipelineId;

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
                _pitch = Mathf.Clamp(value, -3f, 3f);
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
                    value.children.Add(_pipelineId, this);
                else
                    value.children.Remove(_pipelineId);
                UpdateRealProperties();
                UpdateChildrenProperties();
            }
        }

        private Dictionary<int, AudioPipeline> _children;
        public Dictionary<int, AudioPipeline> children => _children;

        public void UpdateRealProperties()
        {
            _realVolume = _volume * (parent != null ? parent.realVolume : 1f);
            _realPitch = _pitch * (parent != null ? parent.realPitch : 1f);
            _realMute = _mute || (parent != null && parent.realMute);
            _behavior?.SetVolume(_realVolume, 0f);
            _behavior?.SetPitch(_realPitch, 0f);
            _behavior?.SetMute(_realMute);
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
            if (request.pipelineId == _pipelineId && _behavior != null)
            {
                _behavior.ReceiveRequest(request);
                return true;
            }
            if (_children.TryGetValue(request.pipelineId, out var childPipeline))
            {
                return childPipeline.PushRequest(request);
            }
            else
            {
                foreach (var child in _children.Values)
                {
                    if (child.PushRequest(request)) return true;
                }
            }
            return false;
        }

        public bool RemoveRequest(int pipelineId, string clipPath)
        {
            if (pipelineId == _pipelineId && _behavior != null)
            {
                _behavior.RemoveRequest(clipPath);
                return true;
            }
            if (_children.TryGetValue(pipelineId, out var childPipeline))
            {
                return childPipeline.RemoveRequest(pipelineId, clipPath);
            }
            foreach (var child in _children.Values)
            {
                if (child.RemoveRequest(pipelineId, clipPath)) return true;
            }
            return false;
        }

        private RefCountBool _isPause; 
        public bool isPause => _isPause;
        public void Pause()
        {
            if (!_isPause)
            {
                _behavior?.Pause();
                foreach (var child in _children.Values)
                {
                    child.Pause();
                }
            }
            _isPause++;
        }
        
        public void Resume(bool force)
        {
            if (force)
            {
                _isPause.Clear();
            }
            else
            {
                _isPause--;
            }
            if (_isPause) return;

            var topPipline = parent;
            while (topPipline != null)
            {
                if (topPipline.isPause)
                    return;
                topPipline = topPipline.parent;
            }
            
            _behavior?.Resume();
            foreach (var child in _children.Values)
            {
                child.Resume(force);
            }
        }
        
        public void Clear()
        {
            _behavior?.ClearRequests();
            foreach (var child in _children.Values)
            {
                child.Clear();
            }
        }
    }
}