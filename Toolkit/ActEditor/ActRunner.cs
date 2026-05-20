using System.Collections;
using UnityEngine;

namespace PowerCellStudio
{
    public delegate void ActEvent(ActRuntimePlayer target);

    public class ActRunner : MonoBehaviour
    {
        public bool ignoreTimeScale = false;

        private ActAsset _actAsset;

        private ActRuntimePlayer _actRuntimePlayer;

        public enum State
        {
            Empty,
            Loading,
            Playing,
            Pause,
        }

        private State _state = State.Empty;
        public State state => _state;

        public ActEvent OnActPlayStart;
        public ActEvent OnActPlayEnd;

        private IAssetLoader _assetLoader;

        void OnDestroy()
        {
            Cancel();
        }

        public void Play(string actPath, ActRuntimePlayer target)
        {
            _state = State.Loading;
            if (_state != State.Empty)
                Cancel();
            if (_assetLoader != null) AssetUtils.DeSpawnLoader(_assetLoader);
            _assetLoader = AssetUtils.SpawnLoader(gameObject.name);
            _actRuntimePlayer = target;
            _assetLoader.LoadAsync<ActAsset>(actPath, OnActAssetLoaded);
        }

        private void OnActAssetLoaded(ActAsset actAsset)
        {
            _actAsset = actAsset;
            if (_actAsset == null) return;
            for (int i = 0; i < _actAsset.tracks.Count; i++)
            {
                var track = _actAsset.tracks[i];
                for (int j = 0; j < track.clips.Count; j++)
                {
                    var clip = track.clips[j];
                    clip.Prepare(_actRuntimePlayer, _assetLoader, false);
                }
            }
        }

        private void OnEnd(ActAsset actAsset, ActRuntimePlayer target)
        {
            if (actAsset == null) return;
            for (int i = 0; i < actAsset.tracks.Count; i++)
            {
                var track = actAsset.tracks[i];
                for (int j = 0; j < track.clips.Count; j++)
                {
                    var clip = track.clips[j];
                    clip.ReleaseAsset(target);
                }
            }
            OnActPlayEnd?.Invoke(target);
            AssetUtils.DeSpawnLoader(_assetLoader);
            _assetLoader = null;
        }

        public void Cancel()
        {
            if (_state == State.Empty) return;
            if (_actAsset && _actRuntimePlayer)
                AsyncManager.Run(CancelRoutine(_actAsset, _actRuntimePlayer));
            _state = State.Empty;
            _actAsset = null;
            _actRuntimePlayer = null;
        }

        public void Pause()
        {
            if (_actAsset == null) return;
            if (_state == State.Pause || _state == State.Empty) return;
            _state = State.Pause;
        }

        public void Resume()
        {
            if (_actAsset == null) return;
            if (_state != State.Pause) return;
            if (_actAsset.IsReady()) _state = State.Playing;
            else _state = State.Loading;
        }

        private IEnumerator CancelRoutine(ActAsset actAsset, ActRuntimePlayer target)
        {
            while (actAsset == null || !actAsset.IsReady())
            {
                yield return null;
            }

            OnEnd(actAsset, target);
        }

        private void Update()
        {
            switch (_state)
            {
                case State.Empty:
                case State.Pause:
                    break;
                case State.Loading:
                    if (_actAsset != null && _actAsset.IsReady())
                    {
                        _state = State.Playing;
                        OnActPlayStart?.Invoke(_actRuntimePlayer);
                    }
                    break;
                case State.Playing:
                    var dt = ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
                    var isEnd = false;
                    _actAsset.Simulate(dt, _actRuntimePlayer, out isEnd);
                    if (isEnd)
                    {
                        OnEnd(_actAsset, _actRuntimePlayer);
                        _actAsset = null;
                        _actRuntimePlayer = null;
                        _state = State.Empty;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}