using System.Collections;
using UnityEngine;

namespace PowerCellStudio
{
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
        }

        private State _state = State.Empty;
        public State state => _state;

        void OnDestroy()
        {
            Cancel();
        }

        public void Play(string actPath, ActRuntimePlayer target)
        {
            if (_actAsset == null) return;
            _state = State.Loading;
            _actRuntimePlayer = target;
            for (int i = 0; i < _actAsset.tracks.Count; i++)
            {
                var track = _actAsset.tracks[i];
                for (int j = 0; j < track.clips.Count; j++)
                {
                    var clip = track.clips[j];
                    clip.Prepare(target, false);
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
        }

        public void Cancel()
        {
            if (_actAsset == null) return;
            if (_actAsset && _actRuntimePlayer)
                ApplicationManager.RunCoroutine(CancelRoutine(_actAsset, _actRuntimePlayer));
            _state = State.Empty;
            _actAsset = null;
            _actRuntimePlayer = null;
        }

        private IEnumerator CancelRoutine(ActAsset actAsset, ActRuntimePlayer target)
        {
            while (!actAsset.IsReady())
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
                    break;
                case State.Loading:
                    if (_actAsset.IsReady()) _state = State.Playing;
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