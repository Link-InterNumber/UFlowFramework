using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI.Extensions;

namespace PowerCellStudio
{
    public class TimelineActRunner
    {
        private PlayableGraph _playableGraph;
        private Playable _playable;
        private PlayableOutput _playableOutput;

        private bool _paused;

        public LinkEvent onActEnd = new LinkEvent();

        public TimelineActRunner(string runnerName)
        {
            // 初始化PlayableGraph
            _playableGraph = PlayableGraph.Create(runnerName);
            _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        }

        // public void SetUpdateMode(DirectorUpdateMode updateMode)
        // {
            
        // }

        public void SetTimelineAsset(PlayableAsset timelineAsset, GameObject owner)
        {
            // 确保_goraph被初始化
            if (!_playableGraph.IsValid())
                return;
            // 清理现有Graph（如果存在）
            Clear();

            // 创建一个新的Playable
            _playable = timelineAsset.CreatePlayable(_playableGraph, owner);
            // _playable.SetLoopTime(false);
            // var _playableOutput = AnimationPlayableOutput.Create(_playableGraph, "Animation Output", owner.GetOrAddComponent<Animator>());
            // // _playableOutput = ScriptPlayableOutput.Create(_playableGraph, "TimelineOutput");
            // _playableOutput.SetSourcePlayable(_playable);
            _playableGraph.Play();
        }

        public void UpdateManually(float dt)
        {
            if (_paused || !_playableGraph.IsValid()) // && _playableGraph.GetTimeUpdateMode() == DirectorUpdateMode.Manual)
                return;
            // 手动更新Graph
            _playableGraph.Evaluate(dt);
            if (!_playable.IsValid() || !_playable.IsDone() || _playable.GetTime() <= _playable.GetDuration())
                return;
            onActEnd.Invoke();
        }

        public void Clear()
        {
            if (!_playableGraph.IsValid())
                return;
            if (_playableOutput.IsOutputValid()) _playableGraph.DestroyOutput(_playableOutput);
            if (_playable.IsValid()) _playableGraph.DestroyPlayable(_playable);
        }

        public void Pause()
        {
            if (!_playableGraph.IsValid())
                return;
            // 暂停播放
            _playableGraph.Stop();
            _paused = true;
        }

        public void Resume()
        {
            if (!_playableGraph.IsValid())
                return;
            // 恢复播放
            _playableGraph.Play();
            _paused = false;
        }

        public void SetSpeed(float speed)
        {
            if (!_playableGraph.IsValid())
                return;
            // 设置播放速度
            _playableGraph.GetRootPlayable(0).SetSpeed(Mathf.Max(0, speed));
        }
    }
}