using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PowerCellStudio
{
    public class AnimationQueue : LinkPlayableBehaviour
    {
        public float transferTime = 0.5f;
        public int defaultLoop = 1;
        private int _currentClipIndex;
        private float _timeToNextClip;
        private int _currentLoop;
        private AnimationClipContainer _container;
        private ScriptPlayable<AnimationTransfer> _transfer;

        private bool _refreshOnQueueEnd;
        private bool _randomPop;

        private Dictionary<string, int> _loopSetting = new Dictionary<string, int>();

        public static ScriptPlayable<AnimationQueue> Create(PlayableGraph graph, AnimationClipContainer container)
        {
            var playable = ScriptPlayable<AnimationQueue>.Create(graph, 1);
            playable.GetBehaviour().Init(graph, playable, container);
            return playable;
        }

        public void Init(PlayableGraph graph, ScriptPlayable<AnimationQueue> playable, AnimationClipContainer container)
        {
            _playableGraph = graph;
            _playable = playable;
            _container = container;
            _transfer = AnimationTransfer.Create(graph, _container.GetByIndex(0), 0);
            _transfer.SetOutputCount(1);
            _transfer.GetBehaviour().enable = true;
            _container.PlayByIndex(0);
            _playable.SetInputCount(1);
            _playable.ConnectInput(0, _transfer, 0, 1f);
            _timeToNextClip = _container.GetByIndex(0).GetAnimationClip().length;
        }

        public void SetLoop(string clipName, int loopTime)
        {
            loopTime = Mathf.Max(1, loopTime);
            _loopSetting[clipName] = loopTime;
        }

        public override void PrepareFrame(Playable owner, FrameData info)
        {
            if (_container.clips.Count == 0)
                return;

            // 必要时，前进到下一剪辑
            _timeToNextClip -= (float) info.deltaTime;
            if (_timeToNextClip <= transferTime)
            {
                _currentLoop++;
                var clipName = _container.GetByIndex(_currentClipIndex).GetAnimationClip().name;
                if (_loopSetting.TryGetValue(clipName, out var targetLoop) && _currentLoop < targetLoop)
                {
                    _timeToNextClip = _container.GetByIndex(_currentClipIndex).GetAnimationClip().length;
                    return;
                }
                if (_currentLoop < defaultLoop)
                {
                    _timeToNextClip = _container.GetByIndex(_currentClipIndex).GetAnimationClip().length;
                    return;
                }

                _currentLoop = 0;
                _currentClipIndex++;
                if (_currentClipIndex >= _container.clips.Count)
                    _currentClipIndex = 0;
                var currentClip = (AnimationClipPlayable) _container.GetByIndex(_currentClipIndex);

                // 重置时间，以便下一个剪辑从正确位置开始
                currentClip.SetTime(0);
                _timeToNextClip = currentClip.GetAnimationClip().length;
                _transfer.GetBehaviour().TransferTo(currentClip, 0, transferTime);
            }
        }
        
        public override void OnPlayableDestroy(Playable playable)
        {
            base.OnPlayableDestroy(playable);
            _container.Destroy();
            _transfer.Destroy();
            _loopSetting = null;
        }
    }
}