using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PowerCellStudio
{
    [RequireComponent(typeof(Animator))] // 确保物体上有Animator组件
    public class PlayablesAnimationPlayer : MonoBehaviour
    {
        public AnimationClip clip; // 拖入需要播放的AnimationClip
        public bool playAwake;
        public bool loop;

        private PlayableGraph _playableGraph;
        private AnimationClipPlayable _clipPlayable;
        private AnimationPlayableOutput _output;

        private bool _paused;

        private void OnEnable()
        {
            if (clip == null)
            {
                Debug.LogError("未指定AnimationClip！", this);
                return;
            }

            // 创建PlayableGraph
            _playableGraph = PlayableGraph.Create();
            _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            // 创建AnimationClipPlayable并连接到输出
            _clipPlayable = AnimationClipPlayable.Create(_playableGraph, clip);
            _clipPlayable.SetLoopTime(loop);
            _output = AnimationPlayableOutput.Create(_playableGraph, "AnimationOutput", GetComponent<Animator>());
            _output.SetSourcePlayable(_clipPlayable);

            // 播放动画
            if (playAwake) _playableGraph.Play();
        }

        private void OnDisable()
        {
            // 销毁PlayableGraph，避免内存泄漏
            if (_playableGraph.IsValid())
                _playableGraph.Destroy();
        }

        private void Update()
        {
            if (!_clipPlayable.IsValid())
                return;
            if (_clipPlayable.GetTime() <= _clipPlayable.duration)
                return;
            
        }

        public void SetUpdateMode(DirectorUpdateMode updateMode)
        {
            _playableGraph.SetTimeUpdateMode(updateMode);
        }

        public void UpdateManually(float dt)
        {
            if (!_paused && _playableGraph.IsValid() && _playableGraph.GetTimeUpdateMode() == DirectorUpdateMode.Manual)
            {
                // 手动更新Graph
                _playableGraph.Evaluate(dt);
            }
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

        public void ResetClip(AnimationClip newClip, bool isloop)
        {
            if (clip == null)
            {
                Debug.LogError("未指定AnimationClip！", this);
                return;
            }
            clip = newClip;
            loop = isloop;
            if (_clipPlayable.IsValid()) _playableGraph.DestroyPlayable(_clipPlayable);
            _clipPlayable = AnimationClipPlayable.Create(_playableGraph, clip);
            _output.SetSourcePlayable(_clipPlayable);
            _clipPlayable.SetTime(0);
            _clipPlayable.SetLoopTime(isloop);
        }

        public void Replay()
        {
            if (_playableGraph.IsValid() && _clipPlayable.IsValid())
            {
                // 重置播放时间为0
                _clipPlayable.SetTime(0);

                // 确保PlayableGraph在播放状态
                _playableGraph.Play();
            }
        }
    }
}