using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PowerCellStudio
{
    public class SongDisc : IDisposable
    {
        private AudioClip[] _clips;
        private int _index;
        private bool _isRandom;
        public float fadeoutTime = 1f;
        public float intervalTime = 1f;
        public float fadeinTime = 1f;
        public bool restart;
        
        public LinkEvent<SongDisc> onClipLoaded = new LinkEvent<SongDisc>();
        private IAssetLoader _assetLoader;
        private HashSet<string> _clipRefs;

        public SongDisc(string[] clipRefs, bool isRandom)
        {
            _assetLoader = AssetUtils.SpawnLoader("SongDisc");
            _clipRefs = new HashSet<string>(clipRefs);
            ApplicationManager.instance.StartCoroutine(LoadAudioClip(clipRefs));
            _isRandom = isRandom;
            _index = 0;
        }
        
        public bool IsSame(string[] clipRefs)
        {
            if (_clipRefs == null || _clipRefs.Count != clipRefs.Length) return false;
            foreach (var clipRef in clipRefs)
            {
                if (!_clipRefs.Contains(clipRef)) return false;
            }
            return true;
        }
        
        private IEnumerator LoadAudioClip(string[] clipRefs)
        {
            _clips = new AudioClip[clipRefs.Length];
            for (var i = 0; i < clipRefs.Length; i++)
            {
                var request = _assetLoader.LoadAsYieldInstruction<AudioClip>(clipRefs[i]);
                yield return request;
                _clips[i] = request.asset;
            }
            onClipLoaded?.Invoke(this);
        }

        public void SetClips(string[] clipRefs, bool isRandom)
        {
            Dispose();
            _assetLoader = AssetUtils.SpawnLoader("SongDisc");
            _clipRefs.Clear();
            for (var i = 0; i < clipRefs.Length; i++)
            {
                _clipRefs.Add(clipRefs[i]);
            }
            _isRandom = isRandom;
            _index = 0;
            ApplicationManager.instance.StartCoroutine(LoadAudioClip(clipRefs));
        }
        
        public void Restart()
        {
            SetCurrent(0);
        }
        
        public void SetCurrent(int index)
        {
            if (_clips == null || _clips.Length == 0) return;
            if (index < 0 || index >= _clips.Length) return;
            _index = index;
        }
        
        public bool CanGetNextClip(float playedTime)
        {
            if (_clips == null || _clips.Length < 2) return false;
            var curClip = GetCurrentClip();
            if (!curClip)
            {
                var nextClip = GetNextClip();
                return nextClip;
            }
            return playedTime >= curClip.length - fadeoutTime;
        }
        
        public AudioClip GetNextClip()
        {
            if (_clips == null || _clips.Length == 0) return null;
            if(_clips.Length == 1) return _clips[0];
            if (_isRandom && _clips.Length > 1)
            {
                var index = Random.Range(0, _clips.Length -1);
                if(_index >= index)
                {
                    index++;
                    if (index >= _clips.Length)
                    {
                        index = 0;
                    }
                    _index = index;
                }
            }
            else
            {
                _index++;
                if (_index >= _clips.Length)
                {
                    _index = 0;
                }
            }
            return _clips[_index];
        }
        
        public AudioClip GetCurrentClip()
        {
            if (_clips == null || _clips.Length == 0 || _index < 0 || _index > _clips.Length -1) return null;
            return _clips[_index];
        }

        public void Dispose()
        {
            AssetUtils.DeSpawnLoader(_assetLoader);
            _assetLoader = null;
            _clips = null;
        }
    }
}