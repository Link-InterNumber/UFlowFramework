using System;
using System.Collections;
using UnityEngine;

namespace PowerCellStudio
{

    public class LinkAudioSource : PoolMono
    {
        [Range(0f,1f)]
        public float setVolume = 1f;
        [Range(-3f,3f)]
        public float setPitch = 1f;
        public bool autoDespawn;

        private AudioSource _audioSource;
        public AudioSource audioSource => _audioSource;
        
        public LinkEvent onFree = new LinkEvent();
        
        private IAssetLoader _assetLoader;

        public float currentVolume => _onGoingRequest.volume <= 0f ? 1 : _onGoingRequest.volume;

        public override void OnSpawn()
        {
            if (!_audioSource)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }
            gameObject.SetActive(true);
            _assetLoader = AssetUtils.SpawnLoader();
        }

        public override void OnDeSpawn()
        {
            AssetUtils.DeSpawnLoader(_assetLoader);
            _assetLoader = null;
            if (!string.IsNullOrEmpty(onGoingRequest.clipPath)) onFree?.Invoke();
            onFree.RemoveAllListeners();
            ClearRequest();
            gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            AssetUtils.DeSpawnLoader(_assetLoader);
            _assetLoader = null;
            StopAllCoroutines();
            if (string.IsNullOrEmpty(onGoingRequest.clipPath)) return;
            onFree?.Invoke();
            onFree = null;
        }

        public void Pause()
        {
            Fade(0, 0.3f, _audioSource.Pause);
        }

        public void Resume()
        {
            _audioSource.UnPause();
            Fade(currentVolume, 0.3f);
        }

        private Coroutine _fadeCoroutine;
        public void Fade(float targetVolume, float duration, Action onComplete = null)
        {
            if (duration <= 0)
            {
                _audioSource.volume = targetVolume * setVolume;
                onComplete?.Invoke();
                return;
            }
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeHelper(targetVolume, duration, onComplete));
        }

        private IEnumerator FadeHelper(float targetVolume, float duration, Action onComplete = null)
        {
            float startVolume = _audioSource.volume;
            float timeElapsed = 0f;
            var target = targetVolume * setVolume;
            while (timeElapsed < duration)
            {
                timeElapsed += Time.unscaledDeltaTime;
                float lerpValue = timeElapsed / duration;
                _audioSource.volume = Mathf.Lerp(startVolume, target, lerpValue);
                yield return null;
            }
            _audioSource.volume = target;
            onComplete?.Invoke();
            _fadeCoroutine = null;
        }

        private Coroutine _fadePitchCoroutine;
        public void FadePitch(float targetPitch, float duration, Action onComplete = null)
        {
            if (duration <= 0)
            {
                _audioSource.pitch = targetPitch * setPitch;
                onComplete?.Invoke();
                return;
            }
            if (_fadePitchCoroutine != null)
                StopCoroutine(_fadePitchCoroutine);
            _fadePitchCoroutine = StartCoroutine(FadePitchHelper(targetPitch, duration, onComplete));
        }
        
        private IEnumerator FadePitchHelper(float targetPitch, float duration, Action onComplete = null)
        {
            float startPitch = _audioSource.pitch;
            float timeElapsed = 0f;
            var target = targetPitch * setPitch;
            while (timeElapsed < duration)
            {
                timeElapsed += Time.unscaledDeltaTime;
                float lerpValue = timeElapsed / duration;
                _audioSource.pitch = Mathf.Lerp(startPitch, target, lerpValue);
                yield return null;
            }
            _audioSource.pitch = target;
            onComplete?.Invoke();
            _fadePitchCoroutine = null;
        }

#if UNITY_EDITOR
        [SerializeField]
#endif
        private AudioRequest _onGoingRequest;
        public AudioRequest onGoingRequest => _onGoingRequest;
        public void Play(AudioRequest request)
        {
            if (string.IsNullOrEmpty(request.clipPath))
            {
                if (request.fadeIn > 0)
                {
                    Fade(request.volume, request.fadeIn);
                }
                else if (request.fadeOut > 0)
                {
                    Fade(0, request.fadeOut);
                }
                return;
            }
            if (onGoingRequest.fadeOut > 0 && _onGoingRequest.clipPath != request.clipPath)
            {
                var fadeTime = Mathf.Min(onGoingRequest.fadeOut, GetClipLength() - GetCurrentTime());
                Fade(0f, fadeTime, () => {
                    _assetLoader.LoadAsync<AudioClip>(request.clipPath, clip =>
                    {
                        RunRequest(request, clip);
                    });
                });
                return;
            }
            _assetLoader.LoadAsync<AudioClip>(request.clipPath, clip =>
            {
                RunRequest(request, clip);
            });
        }
        
        public void FadeOutAndClear()
        {
            if (!gameObject.activeInHierarchy) return;
            if (onGoingRequest.fadeOut > 0)
            {
                var fadeTime = Mathf.Min(onGoingRequest.fadeOut, GetClipLength() - GetCurrentTime());
                Fade(0, fadeTime, ClearRequest);
                return;
            }
            ClearRequest();
        }

        public void FadeOutAndDespawn()
        {
            if (!gameObject.activeInHierarchy) return;
            if (onGoingRequest.fadeOut > 0)
            {
                var fadeTime = Mathf.Min(onGoingRequest.fadeOut, GetClipLength() - GetCurrentTime());
                Fade(0, fadeTime, DeSpawn);
                return;
            }
            DeSpawn();
        }

        private void RunRequest(AudioRequest request, AudioClip clip)
        {
            if (!string.IsNullOrEmpty(_onGoingRequest.clipPath)
                && _onGoingRequest.clipPath == request.clipPath)
            {
                _onGoingRequest = request;
                if (_audioSource.loop && !request.loop)
                {
                    _audioSource.time = GetCurrentTime();
                    _audioSource.Play();
                }
                _audioSource.loop = request.loop;
                return;
            }

            if (!string.IsNullOrEmpty(_onGoingRequest.clipPath))
                _assetLoader?.Release(_onGoingRequest.clipPath);
            _onGoingRequest = request;
            _audioSource.loop = request.loop;
            _audioSource.volume = request.fadeIn > 0f ? 0f : setVolume * currentVolume;
            _audioSource.spatialBlend = request.full3D ? 1f : 0f;
            if (request.full3D) transform.position = request.position;
            if (request.attachGameObject)
            {
                transform.SetParent(request.attachGameObject.transform);
                transform.localPosition = Vector3.zero;
            }
            _audioSource.clip = clip;
            _audioSource.time = 0f;
            if (_onGoingRequest.fadeIn > 0)
            {
                if (request.delay > 0f) _audioSource.PlayDelayed(request.delay);
                else _audioSource.Play();
                Fade(currentVolume, _onGoingRequest.fadeIn);
            }
            else
            {
                if (request.delay > 0f) _audioSource.PlayDelayed(request.delay);
                else _audioSource.Play();
            }
        }

        public void ClearRequest()
        {
            if(_onGoingRequest.attachGameObject)
            {
                transform.SetParent(AudioManager.instance.transform);
                transform.localPosition = Vector3.zero;
            }
            _onGoingRequest = default(AudioRequest);
            _audioSource.clip = null;
            _fadePitchCoroutine = null;
            _fadeCoroutine = null;
            StopAllCoroutines();
        }

        public bool IsReachedEnd()
        {
            if (!_audioSource.clip) return false;
            if (_audioSource.loop)
            {
                return _audioSource.time + Time.unscaledDeltaTime >= _audioSource.clip.length;
            }
            return _audioSource.time >= _audioSource.clip.length;
        }

        public float GetCurrentTime()
        {
            if (!_audioSource.clip) return 0f;
            return _audioSource.time % _audioSource.clip.length;
        }
        
        public float GetPlayedTime()
        {
            if (!_audioSource.clip) return 0f;
            return _audioSource.time;
        }

        public float GetClipLength()
        {
            if (!_audioSource.clip) return 0f;
            return _audioSource.clip.length;
        }

        private void UpdateFadeOut()
        {
            if (_onGoingRequest.loop || _onGoingRequest.fadeOut <= 0 || _fadeCoroutine != null) return;
            var clipLength = GetClipLength();
            if (clipLength == 0) return;
            var currentTime = GetCurrentTime();
            if (currentTime < clipLength - _onGoingRequest.fadeOut) return;
            var lerp = (clipLength - currentTime) / _onGoingRequest.fadeOut;
            _audioSource.volume = Mathf.Lerp(setVolume * onGoingRequest.volume, 0f, lerp);
        }

        private void Update()
        {
            if (!_audioSource || !_audioSource.clip) return;
            UpdateFadeOut();
            if (_onGoingRequest.loop || !IsReachedEnd()) return;
            if (autoDespawn)
                DeSpawn();
            else
            {
                if (!string.IsNullOrEmpty(_onGoingRequest.clipPath))
                    _assetLoader.Release(_onGoingRequest.clipPath);
                ClearRequest();
                onFree?.Invoke();
            }
        }
    }
}