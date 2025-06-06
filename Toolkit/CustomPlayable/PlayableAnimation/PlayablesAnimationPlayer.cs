using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PowerCellStudio
{
    [RequireComponent(typeof(Animator))] // 确保物体上有Animator组件
    public class PlayablesAnimationPlayer : MonoBehaviour
    {
        public AnimationClip[] clips; // 拖入需要播放的AnimationClip
        public bool playAwake;

        private PlayableGraph _playableGraph;
        private AnimationClipPlayable _clipPlayable;
        private AnimationPlayableOutput _output;
        private int _currentIndex = -1;

        private bool _paused;

        private LinkEvent _onPlayEnd = new LinkEvent();

        private void OnEnable()
        {
            if (clip == null)
            {
                LinkLog.LogError("未指定AnimationClip！", this);
                return;
            }

            // 播放动画
            if (playAwake && clips.Length > 0) 
                Play(1);
        }

        public void Play(string clipName)
        {
            if (clips == null)
            {
                return;
            }
            for (var i = 0; i < clips.Length; i++)
            {
                var clip = clips[index];
                if (!clip) continue;
                if (!clip.name.Equals(clipName)) continue;
                Play(i);
                return;
            }
            LinkLog.LogWarning($"clips do not contains [{clipName}]!");
        }

        public void Play(int index)
        {
            if (clips == null || index < 0 || index > clips.Length - 1)
            {
                LinkLog.LogError("index out of clips length!");
                return;
            }
            if (_currentIndex == index) return;
            var clip = clips[index];
            if (!clip)
            {
                LinkLog.LogWarning($"clips[{index}] is null!");
                return;
            }
            _currentIndex = index;
            PlayableHandler(clip);
        }

        private void PlayableHandler(AnimationClip clip)
        {
            if(!_playableGraph.IsValid())
            {
                // 创建PlayableGraph
                _playableGraph = PlayableGraph.Create();
                _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            }

            if (_clipPlayable.IsValid())
            {
                _playableGraph.DestroyPlayable(_clipPlayable);
            }
            // 创建AnimationClipPlayable并连接到输出
            _clipPlayable = AnimationClipPlayable.Create(_playableGraph, clip);

            if (!_output.IsValid())
            {
                _output = AnimationPlayableOutput.Create(_playableGraph, "AnimationOutput", GetComponent<Animator>());
            }
            _output.SetSourcePlayable(_clipPlayable);

            _playableGraph.Play();
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
            if (_clipPlayable.GetTime() <= _clipPlayable.GetDuration())
                return;
            _onPlayEnd.Invoke();
            _clipPlayable.SetTime(0);
            if (!clip.loop) _playableGraph.Pause();
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

        public void PlayClip(AnimationClip newClip)
        {
            if (newClip == null)
            {
                return;
            }
            PlayableHandler(newClip);
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