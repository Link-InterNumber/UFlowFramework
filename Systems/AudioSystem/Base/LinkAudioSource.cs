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
        
        public delegate void OnRemoveClip(string clipPath, bool onDespawn);
        public event OnRemoveClip onRemoveClip;
        public delegate void OnReachEnd(string clipPath);
        public event OnReachEnd onReachEnd;

        public float currentVolume => _onGoingRequest.volume <= 0f ? 1 : _onGoingRequest.volume;

        public override void OnSpawn()
        {
            if (!_audioSource)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }
            gameObject.SetActive(true);
            onReachEnd = null;
        }

        public override void OnDeSpawn()
        {
            onRemoveClip?.Invoke(onGoingRequest.clipPath, true);
            onRemoveClip = null;
            onReachEnd = null;
            ClearRequest();
            gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            StopAllCoroutines();
            if (string.IsNullOrEmpty(onGoingRequest.clipPath)) return;
            onRemoveClip?.Invoke(onGoingRequest.clipPath, true);
            onRemoveClip = null;
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
        public void Play(AudioRequest request, AudioClip clip)
        {
            if (clip == null) return;
            if (onGoingRequest.fadeOut > 0 && _onGoingRequest.clipPath != request.clipPath)
            {
                var fadeTime = Mathf.Min(onGoingRequest.fadeOut, GetClipLength() - GetCurrentTime());
                Fade(0f, fadeTime, () => {
                    RunRequest(request, clip);
                });
                return;
            }
            RunRequest(request, clip);
        }

        public void FadeOutAndDespawn()
        {
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
                _audioSource.loop = request.loop;
                _audioSource.time = GetCurrentTime();
                _audioSource.Play();
                return;
            }
            if (!string.IsNullOrEmpty(_onGoingRequest.clipPath))
                onRemoveClip?.Invoke(onGoingRequest.clipPath, false);
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
            if (!request.loop)
            {
                _audioSource.PlayOneShot(clip);
                return;
            }
            _audioSource.clip = clip;
            _audioSource.time = 0f;
            _audioSource.Play();
        }

        private void ClearRequest()
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
            return _audioSource.time >= _audioSource.clip.length;
        }

        public float GetCurrentTime()
        {
            if (!_audioSource.clip) return 0f;
            return _audioSource.time % _audioSource.clip.length;
        }

        public float GetClipLength()
        {
            if (!_audioSource.clip) return 0f;
            return _audioSource.clip.length;
        }

        private void UpdateFade(float clipLength, float currentTime)
        {
            if (_fadeCoroutine != null) return;
            if (clipLength <= 0f) return;
            if (currentTime < _onGoingRequest.fadeIn)
            {
                var lerpValue = currentTime / _onGoingRequest.fadeIn;
                var targetVolume = setVolume * currentVolume * lerpValue;
                _audioSource.volume = targetVolume;
            }
            else if (!onGoingRequest.loop && clipLength - currentTime < _onGoingRequest.fadeOut)
            {
                var lerpValue = (clipLength - currentTime) / _onGoingRequest.fadeOut;
                var targetVolume = setVolume * _onGoingRequest.volume * lerpValue;
                _audioSource.volume = targetVolume;
            }
        }

        private void Update()
        {
            if (!_audioSource) return;
            var clipLength = GetClipLength();
            var currentTime = GetCurrentTime();
            UpdateFade(clipLength, currentTime);
            if (currentTime >= clipLength && onReachEnd?.GetInvocationList().Length > 0)
            {
                onReachEnd.Invoke(onGoingRequest.clipPath);
                onReachEnd = null;
            }
            if (!autoDespawn || !IsReachedEnd() || _onGoingRequest.loop) return;
            DeSpawn();
        }
    }
}