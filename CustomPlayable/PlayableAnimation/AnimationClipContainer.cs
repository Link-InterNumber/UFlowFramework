using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Random = UnityEngine.Random;

namespace PowerCellStudio
{
    public class AnimationClipContainer : IDisposable
    {
        public List<AnimationClipPlayable> clips;
        private PlayableGraph _playableGraph;

        public static AnimationClipContainer Create(PlayableGraph graph, AnimationClip defaultClip)
        {
            var container = new AnimationClipContainer();
            container.Init(graph, defaultClip);
            return container;
        }

        private void Init(PlayableGraph graph, AnimationClip defaultClip)
        {
            // clips = new List<AnimationClipPlayable>();
            _playableGraph = graph;
            Add(defaultClip);
        }

        private bool VerifyClips()
        {
            if (clips == null || clips.Count == 0)
            {
                Debug.LogError("Please Add Clip First");
                return false;
            }

            return true;
        }
        
        public void SetTime(int index, float time)
        {
            GetClip(index).SetTime(time);
        }

        public void SetTime(string name, float time)
        {
            var match = clips.FindIndex(o => o.GetAnimationClip().name == name);
            if (match > clips.Count - 1 || match < 0) return;
            GetClip(match).SetTime(time);
        }

        public void Add(AnimationClip clip)
        {
            if (clips == null) clips = new List<AnimationClipPlayable>();
            CreateClipPlayable(clip);
        }

        public void ReplaceByIndex(int index, AnimationClip clip, bool restart = false)
        {
            if (!VerifyClips()) return;
            if(!clip) return;
            if (index > clips.Count - 1 || index < 0)
            {
#if UNITY_EDITOR
                throw new IndexOutOfRangeException();
#else
                return;
#endif
            }
            var playableClip = GetClip(index);
            var newClip = AnimationClipPlayable.Create(_playableGraph, clip);
            var isPlaying = playableClip.GetPlayState() == PlayState.Playing;
            if(isPlaying) newClip.Play();
            else newClip.Pause();
            if(restart) newClip.SetTime(0f);
            else
            {
                var time = playableClip.GetTime() % playableClip.GetAnimationClip().length;
                newClip.SetTime(time);
            }
            
            var outputPlayable = playableClip.GetOutput(0);
            if (outputPlayable.IsValid())
            {
                for (int i = 0; i < outputPlayable.GetInputCount(); i++)
                {
                    if(!outputPlayable.GetInput(i).Equals(playableClip)) continue;
                    var weight = outputPlayable.GetInputWeight(i);
                    outputPlayable.DisconnectInput(i);
                    outputPlayable.ConnectInput(i, newClip,0, weight);
                    break;
                }
            }
            playableClip.Destroy();
            clips[index] = newClip;
        }

        public void ReplaceByName(string animationName, AnimationClip clip, bool restart = false)
        {
            var match = clips.FindIndex(o => o.GetAnimationClip().name == animationName);
            if (match > clips.Count - 1 || match < 0) return;
            ReplaceByIndex(match, clip, restart);
        }

        private AnimationClipPlayable CreateClipPlayable(AnimationClip clip)
        {
            var clipPlayable = AnimationClipPlayable.Create(_playableGraph, clip);
            clips.Add(clipPlayable);
            clipPlayable.Pause();
            return clipPlayable;
        }

        public void RemoveByIndex(int index)
        {
            if (index > clips.Count - 1 || index < 0) return;
            var clip = clips[index];
            clip.Destroy();
            clips.RemoveAt(index);
        }

        public void RemoveByName(string animationName)
        {
            if (!VerifyClips()) return;
            var match = clips.FindIndex(o => o.GetAnimationClip().name == animationName);
            if (match > clips.Count - 1 || match < 0) return;
            RemoveByIndex(match);
        }

        public void RemoveByClip(AnimationClip clip)
        {
            if (!VerifyClips()) return;
            var match = clips.FindIndex(o => o.GetAnimationClip() == clip);
            if (match > clips.Count - 1 || match < 0) return;
            RemoveByIndex(match);
        }

        public void RemoveAll()
        {
            if (!VerifyClips()) return;
            var count = clips.Count;
            for (var i = 0; i < count; i++)
            {
                _playableGraph.DestroyPlayable(clips[i]);
            }
            clips.Clear();
        }

        private AnimationClipPlayable GetClip(int index)
        {
            if (index > clips.Count - 1) throw new IndexOutOfRangeException();
            var clip = clips[index];
            return clip;
        }

        private AnimationClipPlayable PlayClip(int index, bool restart)
        {
            var clip = GetClip(index);
            if (restart) clip.SetTime(0);
            clip.Play();
            return clip;
        }

        private void StopClip(int index)
        {
            var clip = GetClip(index);
            clip.SetTime(0);
            clip.Pause();
        }

        public AnimationClipPlayable GetByIndex(int index, bool restart = false)
        {
            if (!VerifyClips()) return default;
            if (index > clips.Count - 1 || index < 0) return default;
            return GetClip(index);
        }

        public AnimationClipPlayable GetByName(string clipName, bool restart = false)
        {
            if (!VerifyClips()) return default;
            var match = clips.FindIndex(o => o.GetAnimationClip().name == clipName);
            if (match > clips.Count - 1 || match < 0) return default;
            return GetClip(match);
        }

        public AnimationClipPlayable PlayByIndex(int index, bool restart = false)
        {
            if (!VerifyClips()) return default;
            if (index > clips.Count - 1 || index < 0) return default;
            return PlayClip(index, restart);
        }

        public AnimationClipPlayable PlayByName(string clipName, bool restart = false)
        {
            if (!VerifyClips()) return default;
            var match = clips.FindIndex(o => o.GetAnimationClip().name == clipName);
            if (match > clips.Count - 1 || match < 0) return default;
            return PlayClip(match, restart);
        }

        private int _curRandIndex = -1;

        public AnimationClipPlayable PlayRandOne()
        {
            if (!VerifyClips()) return default;
            if (clips.Count == 1) return clips[0];
            var rollList = new List<int>();
            for (int i = 0; i < clips.Count; i++)
            {
                if (_curRandIndex == i) continue;
                rollList.Add(i);
            }

            var rolledIndex = Random.Range(0, rollList.Count - 1);
            _curRandIndex = rollList[rolledIndex];
            return PlayClip(_curRandIndex, true);
        }

        public void PlayAll(bool restart)
        {
            if (!VerifyClips()) return;
            for (int i = 0; i < clips.Count; i++)
            {
                PlayClip(i, restart);
            }
        }

        public void StopByIndex(int index)
        {
            if (!VerifyClips()) return;
            if (index > clips.Count - 1 || index < 0) return;
            StopClip(index);
        }

        public void StopByName(string clipName)
        {
            if (!VerifyClips()) return;
            var match = clips.FindIndex(o => o.GetAnimationClip().name == clipName);
            if (match > clips.Count - 1 || match < 0) return;
            StopClip(match);
        }

        public void StopAll()
        {
            if (!VerifyClips()) return;
            for (int i = 0; i < clips.Count; i++)
            {
                StopClip(i);
            }
        }

        public void Dispose()
        {
            if(clips == null) return;
            var count = clips.Count;
            for (var i = 0; i < count; i++)
            {
                _playableGraph.DestroyPlayable(clips[i]);
            }

            clips = null;
        }

        public void Destroy()
        {
            Dispose();
        }
    }
}