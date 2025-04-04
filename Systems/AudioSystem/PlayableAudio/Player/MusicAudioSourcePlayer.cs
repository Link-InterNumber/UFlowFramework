using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PowerCellStudio
{
    public class MusicAudioSourcePlayer : MonoBehaviour, IBgmPlayer
    {
        private Coroutine _seq;
        private AudioSource _audioSource;
        private Transform _parent;
        private MusicGroup _curGroup = MusicGroup.MainScene;
        private float _realVolume;
        private float _curVolume;
        private float _maxVolume;
        private float _lastPlayTime;
        
        private Dictionary<MusicGroup, float> _audioTime = new Dictionary<MusicGroup, float>();
        private Dictionary<MusicGroup, SongDisc> _bgmDiscs = new Dictionary<MusicGroup, SongDisc>();

        public static MusicAudioSourcePlayer Create(Transform parent, string objName)
        {
            var gameObject = new GameObject(objName);
            var player = gameObject.AddComponent<MusicAudioSourcePlayer>();
            player.transform.SetParent(parent);
            player.Init(parent);
            return player;
        }

        private void Init(Transform parent)
        {
            _parent = parent;
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0f;
            _audioSource.loop = true;
            _audioSource.ignoreListenerPause = true;
            _audioSource.playOnAwake = false;
            _realVolume = _audioSource.volume;
        }

        private float _playtime;
        public void Update()
        {
            if(_seq != null || !_audioSource || !_audioSource.clip || !_audioSource.isPlaying) 
                return;
            _playtime += Time.unscaledDeltaTime;
            if (!_bgmDiscs.TryGetValue(_curGroup, out var info)) 
                return;
            if (!info.CanGetNextClip(_playtime)) 
                return;
            
            _playtime = info.fadeinTime;
            var clip = info.GetNextClip();
            if (clip)
            {
                _seq = ApplicationManager.instance.StartCoroutine(PlayBase(clip, 0f, info.fadeoutTime, info.intervalTime, info.fadeinTime));
            }
        }

        private IEnumerator PlayBase(AudioClip clip, float startTime, float fadeoutTime, float intervalTime, float fadeinTime)
        {
            if(!clip)
            {
                _seq = null;
                yield break;
            }
            if (_audioSource.clip)
            {
                var tempFadeoutTime = fadeoutTime;
                var currentVolume = _audioSource.volume;
                while (tempFadeoutTime > 0f)
                {
                    _audioSource.volume = Mathf.Clamp01(tempFadeoutTime / fadeoutTime * currentVolume);
                    tempFadeoutTime -= Time.unscaledDeltaTime;
                    if(!_audioSource) yield break;
                    yield return null;
                }
            }
            if(intervalTime > 0) yield return new WaitForSecondsRealtime(intervalTime);
            if(!_audioSource)
            {
                _seq = null;
                yield break;
            }
            _audioSource.clip = clip;
            _audioSource.time = startTime;
            if (_pausedGroups.Contains(_curGroup))
            {
                _audioSource.Pause();
            }
            else if(!IsMute)
            {
                _audioSource.Play();
            }
            _lastPlayTime = Time.unscaledTime;
            var tempFadeinTime = fadeinTime;
            while (tempFadeinTime > 0f)
            {
                _audioSource.volume = Mathf.Clamp01((1f - tempFadeinTime / fadeoutTime) * _realVolume);
                tempFadeinTime -= Time.unscaledDeltaTime;
                if(!_audioSource)
                {
                    _seq = null;
                    yield break;
                }
                yield return null;
            }
            _seq = null;
        }

        public bool SetCurGroup(MusicGroup group)
        {
            if (_curGroup == group) return true;
            if (!_bgmDiscs.TryGetValue(group, out var info)) return false;
            _audioTime[_curGroup] = _audioSource.time + info.fadeoutTime;
            _curGroup = group;
            var clip = info.GetCurrentClip();
            if (clip)
            {
                if (_seq != null) ApplicationManager.instance.StopCoroutine(_seq);
                var startTime = 0f;
                if (!info.restart && _audioTime.TryGetValue(group, out var playTime))
                {
                    startTime = Mathf.Max(0, playTime + Time.unscaledTime - _lastPlayTime) % clip.length;
                }
                _seq = ApplicationManager.instance.StartCoroutine(PlayBase(clip, startTime, info.fadeoutTime,
                    info.intervalTime, info.fadeinTime));
            }
            return true;
        }

        public bool HasGroup(MusicGroup audioType)
        {
            return _bgmDiscs.ContainsKey(audioType);
        }

        private MusicGroup _onGoingGroup;
        public void Play(string[] clipRefs, MusicGroup group, bool randPlay, bool restart, float fadeoutTime = 1f, float intervalTime = 1f, float fadeinTime = 1f)
        {
            if(clipRefs == null || clipRefs.Length == 0) return;
            _onGoingGroup = group;
            if (_bgmDiscs.TryGetValue(group, out var currentDisc))
            {
                if (currentDisc.IsSame(clipRefs))
                {
                    SetCurGroup(group);
                    return;
                }
                currentDisc.onClipLoaded.AddListener(OnAudioClipLoadCompleted);
                currentDisc.SetClips(clipRefs, randPlay);
                currentDisc.fadeoutTime = fadeoutTime;
                currentDisc.intervalTime = intervalTime;
                currentDisc.fadeinTime = fadeinTime;
                currentDisc.restart = restart;
            }
            else
            {
                var disc = new SongDisc(clipRefs, randPlay);
                disc.onClipLoaded.AddListener(OnAudioClipLoadCompleted);
                disc.fadeoutTime = fadeoutTime;
                disc.intervalTime = intervalTime;
                disc.fadeinTime = fadeinTime;
                disc.restart = restart;
                _bgmDiscs[group] = disc;
            }
        }

        private void OnAudioClipLoadCompleted(SongDisc data)
        {
            data.onClipLoaded.RemoveListener(OnAudioClipLoadCompleted);
            SetCurGroup(_onGoingGroup);
        }

        private HashSet<MusicGroup> _pausedGroups = new HashSet<MusicGroup>();
        public void Pause(MusicGroup group)
        {
            if(!_audioSource) return;
            _pausedGroups.Add(group);
            if (_curGroup == group && _bgmDiscs.TryGetValue(_curGroup, out var gr))
            {
                SetVolume(0, gr.fadeoutTime, () => { _audioSource.Pause(); });
            }
        }

        public void PauseAll()
        {
            if(!_audioSource) return;
            _pausedGroups.Clear();
            _pausedGroups.UnionWith(_bgmDiscs.Keys);
            SetVolume(0, 0.3f, () => { _audioSource.Pause(); });
        }

        public void Resume(MusicGroup group)
        {
            if(!_audioSource) return;
            _pausedGroups.Remove(group);
            if(_curGroup == group && _bgmDiscs.TryGetValue(_curGroup, out var gr))
            {
                _audioSource.Play();
                SetVolume(_curVolume, gr.fadeinTime);
            }
        }

        public void Restart()
        {
            if(!_audioSource) return;
            if(_bgmDiscs.TryGetValue(_curGroup, out var gr))
            {
                SetVolume(0f, gr.fadeoutTime);
                gr.Restart();
                _audioSource.clip = gr.GetCurrentClip();
                _audioSource.time = 0f;
                _audioSource.Play();
            }
        }

        public void Clear(MusicGroup audioType)
        {
            if(!_audioSource) return;
            if(_curGroup == audioType)
            {
                _audioSource.Stop();
                _audioSource.clip = null;
            }

            if (_bgmDiscs.TryGetValue(audioType, out var info))
            {
                info.Dispose();
                _bgmDiscs.Remove(audioType);
                _audioTime.Remove(audioType);
            }
        }

        public void SetSpeed(float speedValue)
        {
            if(!_audioSource) return;
            _audioSource.pitch = speedValue;
        }

        public MusicGroup GetCurGroup()
        {
            return _curGroup;
        }

        private IEnumerator SetVolumeHandler(float targetVolume, float transferTime, Action onComplete = null)
        {
            if(!_audioSource) yield break;
            while (_seq != null)
            {
                yield return null;
            }
            var timePass = 0f;
            var starValue = _audioSource.volume;
            while (timePass < transferTime)
            {
                var normalized = Mathf.Clamp01(timePass / transferTime);
                _audioSource.volume = Mathf.Lerp(starValue, targetVolume, normalized);
                timePass += Time.unscaledDeltaTime;
                if(!_audioSource ||_seq != null) yield break;
                yield return null;
            }
            onComplete?.Invoke();
            if(_audioSource) _audioSource.volume = targetVolume;
        }

        public void SetVolume(float volume, float transferTime, Action onComplete = null)
        {
            if(!_audioSource) return;
            _realVolume = Mathf.Clamp01(volume * _maxVolume);
            _curVolume = volume;
            if (transferTime <= 0)
            {
                _audioSource.volume = _realVolume;
                return;
            }
            ApplicationManager.instance.StartCoroutine(SetVolumeHandler(_realVolume, transferTime, onComplete));
        }
        
        public void SetMaxVolume(float maxVolume)
        {
            _maxVolume = maxVolume;
            SetVolume(_curVolume, 0f);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isReal"></param>
        /// <returns></returns>
        public float GetVolume(bool isReal)
        {
            return isReal? _audioSource.volume : _curVolume;
        }
        
        public float GetMaxVolume()
        {
            return _maxVolume;
        }

        public void Mute(float transferTime)
        {
            if(!_audioSource) return;
            IsMute = true;
            if (transferTime <= 0)
            {
                _audioSource.mute = true;
                return;
            }
            SetVolume(0f, transferTime, () => { _audioSource.mute = true;});
        }

        public void Unmute(float transferTime)
        {
            if (!_audioSource) return;
            IsMute = false;
            if (transferTime <= 0)
            {
                _audioSource.mute = false;
                return;
            }
            _audioSource.mute = false;
            _audioSource.volume = 0f;
            if(_audioSource.clip) _audioSource.Play();
            SetVolume(_curVolume <= 0f ? 1f : _curVolume, transferTime);
        }

        public bool IsMute { get; private set; }

        public void DeInit()
        {
            if (!_audioSource) return;
            _audioSource.Stop();
            Object.Destroy(_audioSource.gameObject);
        }

        public AudioClip GetCurClip(MusicGroup group)
        {
            if (!_audioSource) return null;
            return _bgmDiscs.TryGetValue(group, out var info) ? info.GetCurrentClip() : null;
        }
    }
}