using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;


namespace PowerCellStudio
{
    public class AudioQueue : LinkPlayableBehaviour
    {
        public float FadeoutTime = 3f;
        public float FadeinTime = 2f;
        public float IntervalTime = 1f;
        public int DefaultLoop = 1;
        public bool PlayRandomly = false;
        
        private int _currentClipIndex;
        private float _timeToNextClip;
        private int _currentLoop;
        private AudioClipContainer _container;
        private ScriptPlayable<AudioTransfer> _transfer;

        private bool _refreshOnQueueEnd;
        private bool _onChangeContainer;
        private AudioClipContainer _containerWaitToChange;

        private Dictionary<string, int> _loopSetting = new Dictionary<string, int>();

        public AudioClipContainer GetContainer()
        {
            return _onChangeContainer ? _containerWaitToChange : _container;
        }

        public static ScriptPlayable<AudioQueue> Create(PlayableGraph graph, AudioClipContainer container)
        {
            var playable = ScriptPlayable<AudioQueue>.Create(graph, 1);
            playable.GetBehaviour().Init(graph, playable, container);
            return playable;
        }

        public void Init(PlayableGraph graph, ScriptPlayable<AudioQueue> playable, AudioClipContainer container)
        {
            _playableGraph = graph;
            _playable = playable;
            _container = container;
            _transfer = AudioTransfer.Create(graph, _container.GetByIndex(0), 0);
            _transfer.SetOutputCount(1);
            _transfer.GetBehaviour().enable = true;
            _container.PlayByIndex(0);
            _container.SetTime(0,0f);
            _playable.SetInputCount(1);
            _playable.ConnectInput(0, _transfer, 0, 1f);
            _timeToNextClip = _container.GetByIndex(0).GetClip().length;
        }

        public void ChangeContainer(AudioClipContainer container)
        {
            if(_containerWaitToChange == null || _container.Equals(container) || _containerWaitToChange.clips.Count == 0) return;
            _containerWaitToChange = container;
            _timeToNextClip = FadeoutTime;
            _onChangeContainer = true;
        }

        public void SetLoopTimes(string clipName, int loopTimes)
        {
            loopTimes = Mathf.Max(1, loopTimes);
            _loopSetting[clipName] = loopTimes;
        }
        
        public override void PrepareFrame(Playable owner, FrameData info)
        {
            if (_container.clips.Count == 0)
                return;

            // 必要时，前进到下一剪辑
            _timeToNextClip -= info.deltaTime;
            if (_onChangeContainer)
            {
                OnChangeContainer(_timeToNextClip);
                return;
            }
            
            if (_timeToNextClip <= FadeoutTime)
            {
                _currentLoop++;
                if (CheckReachLoopTimes()) return;

                if (_container.clips.Count == 1)
                {
                    ProcessWhenOneChip();
                    return;
                }
                FadeOutAndFadeIn();
            }
        }

        private void OnChangeContainer(float f)
        {
            if (_timeToNextClip > 0)
            {
                _transfer.SetInputWeight(0, _timeToNextClip / FadeoutTime);
            }
            else if (_timeToNextClip > -IntervalTime)
            {
                _transfer.SetInputWeight(0, 0f);
                _container.SetTime(0, 0f);
            }
            else if (_timeToNextClip > -(FadeinTime + IntervalTime))
            {
                if (_containerWaitToChange != null)
                {
                    _playable.DisconnectInput(0);
                    _transfer.Destroy();
                    _container = _containerWaitToChange;
                    _containerWaitToChange = null;
                    _transfer = AudioTransfer.Create(_playableGraph, _container.GetByIndex(0), 0);
                    _transfer.SetOutputCount(1);
                    _transfer.GetBehaviour().enable = true;
                    _container.PlayByIndex(0);
                    _container.SetTime(0,0f);
                    _playable.ConnectInput(0, _transfer, 0, 1f);
                }
                _transfer.SetInputWeight(0, -(_timeToNextClip + IntervalTime) / FadeinTime);
            }
            else
            {
                _timeToNextClip = _container.GetByIndex(_currentClipIndex).GetClip().length - FadeinTime;
                _transfer.SetInputWeight(0, 1f);
                _currentLoop = 0;
                _onChangeContainer = false;
            }
        }

        private bool CheckReachLoopTimes()
        {
            var clipName = _container.GetByIndex(_currentClipIndex).GetClip().name;
            if (_loopSetting.TryGetValue(clipName, out var targetLoop) && _currentLoop < targetLoop)
            {
                _timeToNextClip = _container.GetByIndex(_currentClipIndex).GetClip().length;
                return true;
            }
            if (_currentLoop < DefaultLoop)
            {
                _timeToNextClip = _container.GetByIndex(_currentClipIndex).GetClip().length;
                return true;
            }
            return false;
        }

        private void FadeOutAndFadeIn()
        {
            _currentLoop = 0;
            if (PlayRandomly)
            {
                var nextClip = _container.PlayRandOne();
                nextClip.SetTime(0);
                _timeToNextClip = nextClip.GetClip().length + FadeoutTime + IntervalTime;
                _transfer.GetBehaviour().TransferTo(nextClip, 0, FadeoutTime, FadeinTime, IntervalTime);
                return;
            }
            
            _currentClipIndex++;
            if (_currentClipIndex >= _container.clips.Count)
                _currentClipIndex = 0;
            var currentClip = _container.GetByIndex(_currentClipIndex);

            // 重置时间，以便下一个剪辑从正确位置开始
            currentClip.SetTime(0);
            _timeToNextClip = currentClip.GetClip().length + FadeoutTime + IntervalTime;
            _transfer.GetBehaviour().TransferTo(currentClip, 0, FadeoutTime, FadeinTime, IntervalTime);
        }
        
        private void ProcessWhenOneChip()
        {
            if (_timeToNextClip > 0)
            {
                _transfer.SetInputWeight(0, _timeToNextClip / FadeoutTime);
            }
            else if (_timeToNextClip > -IntervalTime)
            {
                _transfer.SetInputWeight(0, 0f);
                _container.SetTime(0, 0f);
            }
            else if (_timeToNextClip > -(FadeinTime + IntervalTime))
            {
                _transfer.SetInputWeight(0, -(_timeToNextClip + IntervalTime) / FadeinTime);
            }
            else
            {
                _timeToNextClip = _container.GetByIndex(_currentClipIndex).GetClip().length - FadeinTime;
                _transfer.SetInputWeight(0, 1f);
                _currentLoop = 0;
            }
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            base.OnPlayableDestroy(playable);
            // _container.Destroy();
            _transfer.Destroy();
            _loopSetting = null;
        }
    }
}