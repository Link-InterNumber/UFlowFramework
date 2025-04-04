using System;
using System.Collections;
using UnityEngine;

namespace PowerCellStudio
{
    public class DialogPlayer : MonoBehaviour, IDialogPlayer
    {
        private Coroutine _seq;
        private AudioSource _audioSource;
        private Transform _parent;
        private float _realVolume;
        private float _curVolume;
        private float _maxVolume;
        private AudioClip _targetClip;
        private IAssetLoader _assetLoader;
        private string _clipRef;
        private Action _callback;

        public static DialogPlayer Create(Transform parent, string objName)
        {
            var gameObject = new GameObject(objName);
            var player = gameObject.AddComponent<DialogPlayer>();
            player.transform.SetParent(parent);
            player.Init(parent);
            return player;
        }
        
        private void Init(Transform parent)
        {
            _parent = parent;
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0f;
            _audioSource.loop = false;
            _audioSource.ignoreListenerPause = true;
            _audioSource.playOnAwake = false;
            _realVolume = _audioSource.volume;
            IsMute = false;
        }

        public void DeInit()
        {
            if(_seq != null) ApplicationManager.instance.StopCoroutine(_seq);
            _seq = null;
            GameObject.Destroy(gameObject);
        }
        
        private float _fadeoutTime = 0.3f;
        public float fadeoutTime { get => Mathf.Max(0.1f, _fadeoutTime); set => _fadeoutTime = Mathf.Max(0.1f, value); }
        
        public void PlayDialog(string clipRef, Action callback)
        {
            if(_assetLoader == null) _assetLoader = AssetUtils.SpawnLoader("DialogPlayer");
            StopPlay();
            _callback = callback;
            _assetLoader.LoadAsync<AudioClip>(clipRef, OnLoadedAudioClip);
        }

        private void OnLoadedAudioClip(AudioClip obj)
        {
            _targetClip = obj;
            _seq = ApplicationManager.instance.StartCoroutine(PlayBase(_callback));
        }

        private void StopPlay()
        {
            if(_seq != null) ApplicationManager.instance.StopCoroutine(_seq);
            _seq = null;
            _assetLoader?.Release(_clipRef);
        }

        private IEnumerator PlayBase(Action callback)
        {
            if (_audioSource.isPlaying)
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
            _audioSource.volume = _realVolume;
            _audioSource.PlayOneShot(_targetClip);
            _targetClip = null;
            while (_audioSource.isPlaying)
            {
                if(!_audioSource) break;
                yield return null;
            }
            callback?.Invoke();
            _seq = null;
            _assetLoader.Release(_clipRef);
        }

        public void Pause()
        {
            if(!_audioSource.clip) return;
            _audioSource.Pause();
        }

        public void Resume()
        {
            if(!_audioSource.clip) return;
            _audioSource.Play();
        }

        public void Clear()
        {
            StopPlay();
            SetVolume(0f, fadeoutTime, () =>
            {
                if (_audioSource) _audioSource.Stop();
                _audioSource.clip = null;
                _targetClip = null;
                AssetUtils.DeSpawnLoader(_assetLoader);
                _assetLoader = null;
                _audioSource.volume = _realVolume;
            });
        }

        private IEnumerator SetVolumeHandler(float targetVolume, float transferTime, Action onComplete = null)
        {
            if(!_audioSource) yield break;
            var timePass = 0f;
            var starValue = _audioSource.volume;
            while (timePass < transferTime)
            {
                var normalized = Mathf.Clamp01(timePass / transferTime);
                _audioSource.volume = Mathf.Lerp(starValue, targetVolume, normalized);
                timePass += Time.unscaledDeltaTime;
                if(!_audioSource) yield break;
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

        public float GetVolume(bool isReal)
        {
            return isReal? _audioSource.volume : _curVolume;
        }

        public float GetMaxVolume()
        {
            return _maxVolume;
        }

        public void Restart()
        {
            if (!_audioSource.clip) return;
            _audioSource.time = 0;
            _audioSource.PlayOneShot(_audioSource.clip);
        }

        public void SetSpeed(float speedValue)
        {
            if(!_audioSource) return;
            _audioSource.pitch = speedValue;
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
            SetVolume(_realVolume == 0f ? 1f : _realVolume, transferTime);
        }

        public bool IsMute { get; private set; }
    }
}