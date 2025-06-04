using UnityEngine;
using UnityEngine.Playables;

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
            _playableGraph.SetTimeUpdateMode(updateMode);
        }

        // public void SetUpdateMode(DirectorUpdateMode updateMode)
        // {
            
        // }

        public void SetTimelineAsset(PlayableAsset timelineAsset)
        {
            // 确保_goraph被初始化
            if (!_playableGraph.IsValid())
                return;
            // 清理现有Graph（如果存在）
            Clear();

            // 创建一个新的Playable
            _playable = timelineAsset.CreatePlayable(_playableGraph, null);
            _playable.SetLoopTime(false);
            _playableOutput = ScriptPlayableOutput.Create(_playableGraph, "TimelineOutput");
            _playableOutput.SetSourcePlayable(_playable);
        }

        public void UpdateManually(float dt)
        {
            if (_paused || _playableGraph.IsValid()) // && _playableGraph.GetTimeUpdateMode() == DirectorUpdateMode.Manual)
                return;
            // 手动更新Graph
            _playableGraph.Evaluate(dt);
            if (!_clipPlayable.IsValid() || _clipPlayable.isDone || _clipPlayable.GetTime() <= _clipPlayable.duration)
                return;
            onActEnd.Invoke();
        }

        public void Clear()
        {
            if (!_playableGraph.IsValid())
                return;
            if(_playableOutput.IsValid()) _playableGraph.DestroyOutput(_playableOutput);
            if(_playable.IsValid()) _playableGraph.DestroyPlayable(_playable)
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

        public void SetSpeed(double speed)
        {
            if (!_playableGraph.IsValid())
                return;
            // 设置播放速度
            _playableGraph.GetRootPlayable(0)?.SetSpeed(Mathf.Max(0, speed));
        }
    }
}