using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class MusicRecord
    {
        private int _index;
        private bool _isRandom;
        private float _lastPlayTime;

        private List<AudioRequest> _clips = new List<AudioRequest>();
        private HashSet<string> _clipSet = new HashSet<string>();
        
        public int Count => _clips?.Count??0;

        public void SetRandom(bool isRandom)
        {
            _isRandom = isRandom;
        }

        public bool IsSame(string[] clipRefs)
        {
            if (_clipSet == null || _clipSet.Count != clipRefs.Length) return false;
            foreach (var clipRef in clipRefs)
            {
                if (!_clipSet.Contains(clipRef)) return false;
            }
            return true;
        }

        public void SetClips(AudioRequest[] clipRefs, bool isRandom)
        {
            _clips.Clear();
            _clipSet.Clear();
            AddClips(clipRefs);
            _isRandom = isRandom;
            if (_isRandom)
            {
                Randomizer.Default.Shuffle(_clips);
            }
            _index = 0;
        }

        public void AddClips(AudioRequest[] requests)
        {
            for (var i = 0; i < requests.Length; i++)
            {
                var clipRef = requests[i];
                if (_clipSet.Contains(clipRef.clipPath)) continue;
                _clips.Add(clipRef);
                _clipSet.Add(clipRef.clipPath);
            }
        }

        public void AddClip(AudioRequest request, int index)
        {
            if (_clipSet.Contains(request.clipPath)) return;
            var insertIndex = Mathf.Clamp(index, 0, _clips.Count);
            _clips.Insert(insertIndex, request);
            if (insertIndex <= _index) _index++;
            _clipSet.Add(request.clipPath);
        }

        public void RemoveClip(string clipPath)
        {
            if (_clipSet.Contains(clipPath)) return;
            for (var i = 0; i < _clips.Count; i++)
            {
                var clipRef = _clips[i];
                if (clipRef.clipPath != clipPath) continue;
                _clips.RemoveAt(i);
                if (i <= _index) _index--;
                break;
            }
        }

        public void Restart()
        {
            SetCurrent(0);
        }

        public void SetCurrent(int index)
        {
            if (_clips == null) return;
            if (index < 0 || index >= _clips.Count) return;
            _index = index;
        }

        public AudioRequest GetCurrent()
        {
            if (_clips == null) return default;
            if (_clips.Count == 1) return _clips[0];
            if (_index < 0 || _index >= _clips.Count) return default;
            return _clips[_index];
        }

        public void MoveNext()
        {
            if (_clips == null || _clips.Count == 0) return;
            if (_clips.Count == 1)
            {
                _index = 0;
                return;
            }
            
            var current = _clips[_index];
            if (!current.loop)
            {
                _clips.RemoveAt(_index);
                _clipSet.Remove(current.clipPath);
            }
            else
            {
                _index++;
            }

            if (_index < _clips.Count) return;
            if (_isRandom && _clips.Count > 0)
            {
                Randomizer.Default.Shuffle(_clips);
            }
            _index = 0;
        }

        public void Stop(float playedTime)
        {
            _lastPlayTime = playedTime;
        }

        public AudioRequest GetLastMusic(out float playedTime)
        {
            playedTime = _lastPlayTime;
            return GetCurrentClip();
        }

        public AudioRequest GetCurrentClip()
        {
            if (_clips == null || _clips.Count == 0 || _index < 0 || _index > _clips.Count - 1) return default;
            return _clips[_index];
        }

        public void Clear()
        {
            if (_clips == null || _clipSet == null) return;
            _clips.Clear();
            _clipSet.Clear();
            _index = 0;
        }

        public void Dispose()
        {
            _clips.Clear();
            _clips = null;
            _clipSet.Clear();
            _clipSet = null;
        }
    }
}